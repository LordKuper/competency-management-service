using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NpgsqlTypes;

namespace Competency.OrgStructure;

/// <summary>
/// The employee API. Any signed-in user reads the restricted projection of working employees; global administrators and HR
/// read the full projection of every employee and change employees, under the tree lock and with <c>If-Match</c>,
/// and only global administrators list the candidates for responsible HR.
/// An employee is in every unit of their main and secondary assignments, so a unit's list holds the employees with any assignment in it.
/// Dismissal and deletion cascade to the units the employee heads, their responsible HR assignments and the bound account in the same transaction,
/// and they preview exactly those changes first. A change of assignments likewise takes the employee off the headship of every unit where no assignment of theirs is left.
/// </summary>
internal static class EmployeeEndpoints
{
    private const string Route = "/api/v1/employees";
    private const string Tag = "Employees";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var readers = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        readers.MapGet(string.Empty, ListAsync).WithName("ListEmployees").ProducesProblem(StatusCodes.Status403Forbidden);
        readers.MapGet("/{id:guid}", GetAsync).WithName("GetEmployee").ProducesETag().ProducesProblem(StatusCodes.Status404NotFound);

        var keepers = readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        keepers.MapPost(string.Empty, CreateAsync).WithName("CreateEmployee").ProducesETag();
        keepers.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateEmployee").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        keepers.MapPost("/{id:guid}/move", MoveAsync).WithName("MoveEmployee").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        keepers.MapGet("/{id:guid}/impact", ImpactAsync).WithName("GetEmployeeImpact")
            .ProducesProblem(StatusCodes.Status404NotFound);
        keepers.MapPost("/{id:guid}/dismiss", DismissAsync).WithName("DismissEmployee").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        keepers.MapPost("/{id:guid}/rehire", RehireAsync).WithName("RehireEmployee").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        keepers.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteEmployee")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<PageResponse<EmployeeResponse>>, ValidationProblem, ProblemHttpResult>> ListAsync(
        [AsParameters] EmployeeListQuery query,
        AppDbContext context,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (query.HrCandidate && !actor.IsGlobalAdmin())
        {
            return Rejection.Forbidden();
        }

        var accounts = context.Set<EmployeeAccount>();
        var placements = context.Set<EmployeePlacement>();
        var employees = context.Set<Employee>()
            .AsNoTracking()
            .VisibleTo(actor)
            .WhereIf(query.IsActive is not null, employee => employee.IsActive == query.IsActive)
            .WhereIf(query.HrCandidate, employee => employee.IsActive && accounts.Any(account => account.EmployeeId == employee.Id && account.CanBeHr));
        HashSet<Guid>? scopeUnitIds = null;
        if (query.OrgUnitId is { } unitId)
        {
            var subtreeIds = context.DescendantIds(unitId);
            employees = query.IncludeDescendants
                ? employees.Where(employee => placements.Any(placement => placement.EmployeeId == employee.Id && subtreeIds.Contains(placement.OrgUnitId)))
                : employees.Where(employee => placements.Any(placement => placement.EmployeeId == employee.Id && placement.OrgUnitId == unitId));
            scopeUnitIds = query.IncludeDescendants ? [.. await subtreeIds.ToListAsync(cancellationToken)] : [unitId];
        }

        if (TextSearch.Normalize(query.Q) is { } text)
        {
            employees = employees.Matching(text, accounts, context.Set<EmployeeAssignment>(), query.HrCandidate);
        }

        var page = await employees
            .OrderBy(employee => employee.FullName)
            .ThenBy(employee => employee.Id)
            .Select(EmployeeResponse.ProjectionFor(context, actor))
            .ToPageAsync(query.Page, query.PageSize, cancellationToken);

        return TypedResults.Ok(page with { Items = await context.FillAsync(page.Items, scopeUnitIds, cancellationToken) });
    }

    /// <summary>
    /// Returns the projection for the caller's roles; the <c>ETag</c> header comes with the full projection only, which carries the version.
    /// </summary>
    private static async Task<Results<Ok<EmployeeResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        ICurrentActor actor,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var employee = await context.Set<Employee>()
            .AsNoTracking()
            .VisibleTo(actor)
            .Where(candidate => candidate.Id == id)
            .Select(EmployeeResponse.ProjectionFor(context, actor))
            .FirstOrDefaultAsync(cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        if (employee.Version is { } version)
        {
            response.SetETag(version);
        }

        return TypedResults.Ok((await context.FillAsync([employee], null, cancellationToken))[0]);
    }

    private static async Task<Results<Created<EmployeeResponse>, ValidationProblem>> CreateAsync(
        EmployeeRequest request,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var input = request.Trimmed();
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var unit = await context.Set<OrgUnit>().FirstOrDefaultAsync(candidate => candidate.Id == input.OrgUnitId, cancellationToken);
        if (unit is null)
        {
            return Rejection.Invalid("orgUnitId", "Подразделение не найдено.");
        }

        var employee = new Employee
        {
            LastName = input.LastName,
            FirstName = input.FirstName,
            MiddleName = input.MiddleName,
            Education = input.Education,
            PositionId = input.PositionId,
            OrgUnit = unit,
        };
        var assignmentChange = await AssignmentChange.LoadAsync(context, employee, input, isNew: true, cancellationToken);
        if (await assignmentChange.CheckAsync(unit, cancellationToken) is { } problem)
        {
            return problem;
        }

        context.Add(employee);
        assignmentChange.Apply();

        var created = await SaveAsync(employee, context, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{created.Id}", created);
    }

    private static async Task<Results<Ok<EmployeeResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        EmployeeRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var input = request.Trimmed();
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var employee = await context.Set<Employee>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        var unit = await context.Set<OrgUnit>().FirstOrDefaultAsync(candidate => candidate.Id == input.OrgUnitId, cancellationToken);
        if (unit is null)
        {
            return Rejection.Invalid("orgUnitId", "Подразделение не найдено.");
        }

        var assignmentChange = await AssignmentChange.LoadAsync(context, employee, input, isNew: false, cancellationToken);
        if (await assignmentChange.CheckAsync(unit, cancellationToken) is { } problem)
        {
            return problem;
        }

        ifMatch.ApplyTo(context, employee);
        await ReleaseHeadsWithoutAssignmentAsync(context, id, [unit.Id, .. input.Assignments.Select(assignment => assignment.OrgUnitId)], cancellationToken);
        employee.LastName = input.LastName;
        employee.FirstName = input.FirstName;
        employee.MiddleName = input.MiddleName;
        employee.Education = input.Education;
        employee.PositionId = input.PositionId;
        employee.OrgUnit = unit;
        assignmentChange.Apply();

        return TypedResults.Ok(await SaveAsync(employee, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Moves one assignment, with its position and legal entities, to another active unit. A position the new unit and the units above it do not allow
    /// is added to the new unit's own list in the same transaction. The employee is taken off the headship of the unit they leave when no other assignment of theirs is left there.
    /// </summary>
    private static async Task<Results<Ok<EmployeeResponse>, NotFound, ValidationProblem, ProblemHttpResult>> MoveAsync(
        Guid id,
        MoveEmployeeRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if (request.FromOrgUnitId == request.ToOrgUnitId)
        {
            return Rejection.Invalid("toOrgUnitId", "Выберите другое подразделение.");
        }

        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var employee = await context.Set<Employee>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        var isMain = employee.OrgUnitId == request.FromOrgUnitId && employee.PositionId == request.PositionId;
        var secondary = isMain
            ? null
            : await context.Set<EmployeeAssignment>().FirstOrDefaultAsync(
                assignment => assignment.EmployeeId == id && assignment.OrgUnitId == request.FromOrgUnitId && assignment.PositionId == request.PositionId,
                cancellationToken);
        if (!isMain && secondary is null)
        {
            return Rejection.Invalid("positionId", "У сотрудника нет такого назначения.");
        }

        if (await context.UnitProblemAsync(request.ToOrgUnitId, "Подразделение", cancellationToken) is { } unitProblem)
        {
            return Rejection.Invalid("toOrgUnitId", unitProblem);
        }

        var holdsTarget = (employee.OrgUnitId == request.ToOrgUnitId && employee.PositionId == request.PositionId)
            || await context.Set<EmployeeAssignment>().AnyAsync(
                assignment => assignment.EmployeeId == id && assignment.OrgUnitId == request.ToOrgUnitId && assignment.PositionId == request.PositionId,
                cancellationToken);
        if (holdsTarget)
        {
            return Rejection.Invalid("toOrgUnitId", "У сотрудника уже есть такое назначение: подразделение и должность не должны повторяться.");
        }

        if (!await context.Set<Position>().AnyAsync(position => position.Id == request.PositionId && position.IsActive, cancellationToken))
        {
            return Rejection.Invalid("positionId", "Должность неактивна.");
        }

        ifMatch.ApplyTo(context, employee);
        if (await context.PositionProblemAsync(request.PositionId, request.ToOrgUnitId, cancellationToken) is not null)
        {
            context.Add(new OrgUnitPosition { OrgUnitId = request.ToOrgUnitId, PositionId = request.PositionId });
        }

        var secondaryUnits = await context.Set<EmployeeAssignment>()
            .Where(assignment => assignment.EmployeeId == id)
            .Select(assignment => new { assignment.Id, assignment.OrgUnitId })
            .ToListAsync(cancellationToken);
        if (secondary is null)
        {
            employee.OrgUnitId = request.ToOrgUnitId;
        }
        else
        {
            secondary.OrgUnitId = request.ToOrgUnitId;
        }

        var remainingUnitIds = secondaryUnits.Where(assignment => assignment.Id != secondary?.Id).Select(assignment => assignment.OrgUnitId)
            .Append(employee.OrgUnitId).Append(request.ToOrgUnitId).ToArray();
        await ReleaseHeadsWithoutAssignmentAsync(context, id, remainingUnitIds, cancellationToken);

        context.Entry(employee).Property(nameof(IVersioned.Version)).IsModified = true;
        return TypedResults.Ok(await SaveAsync(employee, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// What dismissing or deleting the employee would change elsewhere; read-only, and computed by the same code that applies it.
    /// </summary>
    private static async Task<Results<Ok<EmployeeImpactResponse>, NotFound>> ImpactAsync(
        Guid id,
        AppDbContext context,
        IEmployeeAccounts accounts,
        IHrAssignments hrAssignments,
        CancellationToken cancellationToken)
    {
        if (!await context.Set<Employee>().AnyAsync(candidate => candidate.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok((await EmployeeImpact.OfAsync(context, accounts, hrAssignments, id, cancellationToken)).ToResponse());
    }

    /// <summary>
    /// Marks the employee as not working, takes them off every unit they head and every responsible HR assignment and blocks their account, all in one transaction;
    /// refused when the account is the last active administrator.
    /// </summary>
    private static async Task<Results<Ok<EmployeeResponse>, NotFound, ProblemHttpResult>> DismissAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        IEmployeeAccounts accounts,
        IHrAssignments hrAssignments,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var employee = await context.Set<Employee>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        if (!employee.IsActive)
        {
            return Rejection.Conflict($"Сотрудник «{employee.FullName}» уже не работает.");
        }

        await accounts.LockAdministratorsAsync(cancellationToken);
        var impact = await EmployeeImpact.OfAsync(context, accounts, hrAssignments, id, cancellationToken);
        if (impact.IsRefused)
        {
            return Rejection.Conflict(EmployeeImpact.LastAdministratorMessage);
        }

        await impact.ApplyAsync(id, accounts, hrAssignments, unbindAccount: false, cancellationToken);
        ifMatch.ApplyTo(context, employee);
        employee.IsActive = false;

        return TypedResults.Ok(await SaveAsync(employee, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Marks a dismissed employee as working again; the account stays blocked, the units keep their new heads and the HR assignments are not restored.
    /// </summary>
    private static async Task<Results<Ok<EmployeeResponse>, NotFound, ProblemHttpResult>> RehireAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var employee = await context.Set<Employee>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        if (employee.IsActive)
        {
            return Rejection.Conflict($"Сотрудник «{employee.FullName}» уже работает.");
        }

        if (await context.UnitProblemAsync(employee.OrgUnitId, "Подразделение", cancellationToken) is { } unitProblem)
        {
            return Rejection.Conflict($"Нельзя вернуть сотрудника «{employee.FullName}» на работу. {unitProblem} Сначала активируйте подразделение или переведите сотрудника в другое.");
        }

        var inactiveAssignmentUnit = await context.Set<EmployeeAssignment>()
            .Where(assignment => assignment.EmployeeId == id && !assignment.OrgUnit.IsActive)
            .Select(assignment => assignment.OrgUnit.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (inactiveAssignmentUnit is not null)
        {
            return Rejection.Conflict($"Нельзя вернуть сотрудника «{employee.FullName}» на работу. Подразделение «{inactiveAssignmentUnit}», в котором у него совмещение, неактивно. Сначала активируйте подразделение или уберите совмещение.");
        }

        ifMatch.ApplyTo(context, employee);
        employee.IsActive = true;

        return TypedResults.Ok(await SaveAsync(employee, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Deletes the employee for good, after taking them off every unit they head and every responsible HR assignment and blocking and detaching their account, all in one transaction;
    /// refused when the account is the last active administrator.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        IEmployeeAccounts accounts,
        IHrAssignments hrAssignments,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var employee = await context.Set<Employee>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        await accounts.LockAdministratorsAsync(cancellationToken);
        var impact = await EmployeeImpact.OfAsync(context, accounts, hrAssignments, id, cancellationToken);
        if (impact.IsRefused)
        {
            return Rejection.Conflict(EmployeeImpact.LastAdministratorMessage);
        }

        await impact.ApplyAsync(id, accounts, hrAssignments, unbindAccount: true, cancellationToken);
        ifMatch.ApplyTo(context, employee);
        context.Remove(employee);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Takes the employee off the headship of every unit where they are left with no assignment, because a unit is headed only by its own employee.
    /// The units are tracked, so their version and audit follow in the same save.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="employeeId">The employee whose assignments change.</param>
    /// <param name="remainingUnitIds">The units of the employee's assignments once the change is applied.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    private static async Task ReleaseHeadsWithoutAssignmentAsync(
        AppDbContext context,
        Guid employeeId,
        IReadOnlyCollection<Guid> remainingUnitIds,
        CancellationToken cancellationToken)
    {
        var headedUnits = await context.Set<OrgUnit>()
            .Where(unit => unit.HeadEmployeeId == employeeId && !remainingUnitIds.Contains(unit.Id))
            .ToListAsync(cancellationToken);
        foreach (var unit in headedUnits)
        {
            unit.HeadEmployeeId = null;
        }
    }

    /// <summary>
    /// Narrows the employees to those whose full name contains the text, or whose name parts and position together match it as Russian full text,
    /// so one query may mix name and position words, the position being that of any of the employee's assignments. A generated column cannot read
    /// another table, so the employee's vector is joined with its position's vector at query time. When picking a responsible HR,
    /// the account's e-mail containing the text matches too.
    /// </summary>
    private static IQueryable<Employee> Matching(
        this IQueryable<Employee> employees,
        string text,
        IQueryable<EmployeeAccount> accounts,
        IQueryable<EmployeeAssignment> assignments,
        bool matchesEmail)
    {
        var pattern = ListRequest.ContainsPattern(text);
        return employees.Where(employee =>
            EF.Functions.ILike(employee.FullName, pattern, ListRequest.LikeEscape)
            || employee.SearchVector.Concat(employee.Position.SearchVector)
                .Matches(EF.Functions.PlainToTsQuery(TextSearch.FullTextConfig, text))
            || assignments.Any(assignment => assignment.EmployeeId == employee.Id
                && employee.SearchVector.Concat(assignment.Position.SearchVector)
                    .Matches(EF.Functions.PlainToTsQuery(TextSearch.FullTextConfig, text)))
            || (matchesEmail && accounts.Any(account => account.EmployeeId == employee.Id && EF.Functions.ILike(account.Email, pattern, ListRequest.LikeEscape))));
    }

    /// <summary>
    /// Saves the employee and reads the response back while the tree lock is still held, so that the body and its <c>ETag</c> come
    /// from one snapshot that no concurrent change or deletion can alter before the commit.
    /// </summary>
    private static async Task<EmployeeResponse> SaveAsync(
        Employee employee,
        AppDbContext context,
        IDbContextTransaction transaction,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        var read = await context.Set<Employee>()
            .AsNoTracking()
            .Where(candidate => candidate.Id == employee.Id)
            .Select(EmployeeResponse.FullProjection(context))
            .SingleAsync(cancellationToken);
        var saved = (await context.FillAsync([read], null, cancellationToken))[0];
        await transaction.CommitAsync(cancellationToken);
        response.SetETag(saved.Version!.Value);
        return saved;
    }
}
