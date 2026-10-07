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
/// The employee API. Any signed-in user reads the restricted projection of working employees; only global administrators
/// read the full projection and change employees, under the tree lock and with <c>If-Match</c>.
/// Dismissal and deletion cascade to the units the employee heads and to the bound account in the same transaction,
/// and administrators preview exactly those changes first. A transfer likewise takes the employee off the headship of the unit they leave.
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

        readers.MapGet(string.Empty, ListAsync).WithName("ListEmployees");
        readers.MapGet("/{id:guid}", GetAsync).WithName("GetEmployee").ProducesETag().ProducesProblem(StatusCodes.Status404NotFound);

        var administrators = readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        administrators.MapPost(string.Empty, CreateAsync).WithName("CreateEmployee").ProducesETag();
        administrators.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateEmployee").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        administrators.MapGet("/{id:guid}/impact", ImpactAsync).WithName("GetEmployeeImpact")
            .ProducesProblem(StatusCodes.Status404NotFound);
        administrators.MapPost("/{id:guid}/dismiss", DismissAsync).WithName("DismissEmployee").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapPost("/{id:guid}/rehire", RehireAsync).WithName("RehireEmployee").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteEmployee")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<PageResponse<EmployeeResponse>>, ValidationProblem>> ListAsync(
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

        var employees = context.Set<Employee>()
            .AsNoTracking()
            .VisibleTo(actor)
            .WhereIf(query.IsActive is not null, employee => employee.IsActive == query.IsActive);
        if (query.OrgUnitId is { } unitId)
        {
            var subtreeIds = context.DescendantIds(unitId);
            employees = query.IncludeDescendants
                ? employees.Where(employee => subtreeIds.Contains(employee.OrgUnitId))
                : employees.Where(employee => employee.OrgUnitId == unitId);
        }

        if (TextSearch.Normalize(query.Q) is { } text)
        {
            employees = employees.Matching(text);
        }

        var page = await employees
            .OrderBy(employee => employee.FullName)
            .ThenBy(employee => employee.Id)
            .Select(EmployeeResponse.ProjectionFor(context, actor))
            .ToPageAsync(query.Page, query.PageSize, cancellationToken);

        return TypedResults.Ok(page);
    }

    /// <summary>
    /// Returns the projection for the caller's role; the <c>ETag</c> header is sent to global administrators only, because only they may change employees.
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

        return TypedResults.Ok(employee);
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
            return Rejections.Invalid("orgUnitId", "Подразделение не найдено.");
        }

        if (!unit.IsActive)
        {
            return Rejections.Invalid("orgUnitId", "Подразделение неактивно.");
        }

        var employee = new Employee
        {
            LastName = input.LastName,
            FirstName = input.FirstName,
            MiddleName = input.MiddleName,
            Position = input.Position,
            OrgUnit = unit,
        };
        context.Add(employee);

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
            return Rejections.Invalid("orgUnitId", "Подразделение не найдено.");
        }

        if (unit.Id != employee.OrgUnitId && !unit.IsActive)
        {
            return Rejections.Invalid("orgUnitId", "Подразделение неактивно.");
        }

        ifMatch.ApplyTo(context, employee);
        if (unit.Id != employee.OrgUnitId)
        {
            await ReleaseHeadOfPreviousUnitAsync(context, employee, cancellationToken);
        }

        employee.LastName = input.LastName;
        employee.FirstName = input.FirstName;
        employee.MiddleName = input.MiddleName;
        employee.Position = input.Position;
        employee.OrgUnit = unit;

        return TypedResults.Ok(await SaveAsync(employee, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// What dismissing or deleting the employee would change elsewhere; read-only, and computed by the same code that applies it.
    /// </summary>
    private static async Task<Results<Ok<EmployeeImpactResponse>, NotFound>> ImpactAsync(
        Guid id,
        AppDbContext context,
        IEmployeeAccounts accounts,
        CancellationToken cancellationToken)
    {
        if (!await context.Set<Employee>().AnyAsync(candidate => candidate.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok((await EmployeeImpact.OfAsync(context, accounts, id, cancellationToken)).ToResponse());
    }

    /// <summary>
    /// Marks the employee as not working, takes them off every unit they head and blocks their account, all in one transaction;
    /// refused when the account is the last active administrator.
    /// </summary>
    private static async Task<Results<Ok<EmployeeResponse>, NotFound, ProblemHttpResult>> DismissAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        IEmployeeAccounts accounts,
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
            return Rejections.Conflict($"Сотрудник «{employee.FullName}» уже не работает.");
        }

        await accounts.LockAdministratorsAsync(cancellationToken);
        var impact = await EmployeeImpact.OfAsync(context, accounts, id, cancellationToken);
        if (impact.IsRefused)
        {
            return Rejections.Conflict(EmployeeImpact.LastAdministratorMessage);
        }

        await impact.ApplyAsync(id, accounts, unbindAccount: false, cancellationToken);
        ifMatch.ApplyTo(context, employee);
        employee.IsActive = false;

        return TypedResults.Ok(await SaveAsync(employee, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Marks a dismissed employee as working again; the account stays blocked and the units keep their new heads.
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
            return Rejections.Conflict($"Сотрудник «{employee.FullName}» уже работает.");
        }

        if (await context.UnitProblemAsync(employee.OrgUnitId, "Подразделение", cancellationToken) is { } unitProblem)
        {
            return Rejections.Conflict($"Нельзя вернуть сотрудника «{employee.FullName}» на работу. {unitProblem} Сначала активируйте подразделение или переведите сотрудника в другое.");
        }

        ifMatch.ApplyTo(context, employee);
        employee.IsActive = true;

        return TypedResults.Ok(await SaveAsync(employee, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Deletes the employee for good, after taking them off every unit they head and blocking and detaching their account, all in one transaction;
    /// refused when the account is the last active administrator.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        IEmployeeAccounts accounts,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var employee = await context.Set<Employee>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        await accounts.LockAdministratorsAsync(cancellationToken);
        var impact = await EmployeeImpact.OfAsync(context, accounts, id, cancellationToken);
        if (impact.IsRefused)
        {
            return Rejections.Conflict(EmployeeImpact.LastAdministratorMessage);
        }

        await impact.ApplyAsync(id, accounts, unbindAccount: true, cancellationToken);
        ifMatch.ApplyTo(context, employee);
        context.Remove(employee);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Takes the employee off the headship of the unit they work in, if they head it, because a unit is headed only by its own employee.
    /// The unit is tracked, so its version and audit follow in the same save.
    /// </summary>
    private static async Task ReleaseHeadOfPreviousUnitAsync(AppDbContext context, Employee employee, CancellationToken cancellationToken)
    {
        var headedUnit = await context.Set<OrgUnit>()
            .FirstOrDefaultAsync(unit => unit.Id == employee.OrgUnitId && unit.HeadEmployeeId == employee.Id, cancellationToken);
        if (headedUnit is not null)
        {
            headedUnit.HeadEmployeeId = null;
        }
    }

    private static IQueryable<Employee> Matching(this IQueryable<Employee> employees, string text)
    {
        var pattern = TextSearch.ContainsPattern(text);
        return employees.Where(employee =>
            EF.Functions.ILike(employee.FullName, pattern, TextSearch.LikeEscape)
            || employee.SearchVector.Matches(EF.Functions.PlainToTsQuery(TextSearch.FullTextConfig, text)));
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
        var saved = await context.Set<Employee>()
            .AsNoTracking()
            .Where(candidate => candidate.Id == employee.Id)
            .Select(EmployeeResponse.FullProjection(context))
            .SingleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        response.SetETag(saved.Version!.Value);
        return saved;
    }
}
