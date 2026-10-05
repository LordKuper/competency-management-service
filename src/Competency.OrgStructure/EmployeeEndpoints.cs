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
/// Employees are never deleted: leaving the organization is the inactive status.
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
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        administrators.MapPost(string.Empty, CreateAsync).WithName("CreateEmployee").ProducesETag();
        administrators.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateEmployee").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
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
            employees = employees.Matching(text, actor.IsGlobalAdmin());
        }

        var page = await employees
            .OrderBy(employee => employee.FullName)
            .ThenBy(employee => employee.Id)
            .Select(EmployeeResponse.ProjectionFor(actor))
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
            .Select(EmployeeResponse.ProjectionFor(actor))
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

    private static async Task<Results<Created<EmployeeResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
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

        if (await FindDuplicateAsync(context, input, Guid.Empty, cancellationToken) is { } duplicate)
        {
            return Rejections.Conflict(duplicate);
        }

        var employee = new Employee
        {
            FullName = input.FullName,
            PersonnelNumber = input.PersonnelNumber,
            Email = input.Email,
            Position = input.Position,
            IsActive = input.IsActive,
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

        if ((input.IsActive || unit.Id != employee.OrgUnitId) && !unit.IsActive)
        {
            return Rejections.Invalid("orgUnitId", "Подразделение неактивно.");
        }

        if (employee.IsActive && !input.IsActive)
        {
            var headedUnitName = await context.Set<OrgUnit>()
                .Where(headed => headed.HeadEmployeeId == id && headed.IsActive)
                .Select(headed => headed.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (headedUnitName is not null)
            {
                return Rejections.Conflict($"Нельзя перевести сотрудника «{employee.FullName}» в статус «не работает»: он руководитель активного подразделения «{headedUnitName}». Сначала назначьте другого руководителя.");
            }
        }

        if (await FindDuplicateAsync(context, input, id, cancellationToken) is { } duplicate)
        {
            return Rejections.Conflict(duplicate);
        }

        ifMatch.ApplyTo(context, employee);
        employee.FullName = input.FullName;
        employee.PersonnelNumber = input.PersonnelNumber;
        employee.Email = input.Email;
        employee.Position = input.Position;
        employee.IsActive = input.IsActive;
        employee.OrgUnit = unit;

        return TypedResults.Ok(await SaveAsync(employee, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Says why the personnel number or e-mail of a request belongs to another employee, ignoring letter case in the e-mail.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="input">The trimmed request.</param>
    /// <param name="excludedId">The employee being updated, or <see cref="Guid.Empty"/> when creating one.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The reason, or <see langword="null"/> when both values are free.</returns>
    private static async Task<string?> FindDuplicateAsync(
        AppDbContext context,
        EmployeeRequest input,
        Guid excludedId,
        CancellationToken cancellationToken)
    {
        var others = context.Set<Employee>().AsNoTracking().Where(employee => employee.Id != excludedId);
        if (await others.AnyAsync(employee => employee.PersonnelNumber == input.PersonnelNumber, cancellationToken))
        {
            return "Сотрудник с таким табельным номером уже существует.";
        }

        var normalizedEmail = input.Email.ToLowerInvariant();
        return await others.AnyAsync(employee => employee.NormalizedEmail == normalizedEmail, cancellationToken)
            ? "Сотрудник с таким e-mail уже существует."
            : null;
    }

    private static IQueryable<Employee> Matching(this IQueryable<Employee> employees, string text, bool includePersonnelNumber)
    {
        var pattern = TextSearch.ContainsPattern(text);
        return includePersonnelNumber
            ? employees.Where(employee =>
                EF.Functions.ILike(employee.FullName, pattern, TextSearch.LikeEscape)
                || EF.Functions.ILike(employee.Email, pattern, TextSearch.LikeEscape)
                || EF.Functions.ILike(employee.PersonnelNumber, pattern, TextSearch.LikeEscape)
                || employee.SearchVector.Matches(EF.Functions.PlainToTsQuery(TextSearch.FullTextConfig, text)))
            : employees.Where(employee =>
                EF.Functions.ILike(employee.FullName, pattern, TextSearch.LikeEscape)
                || EF.Functions.ILike(employee.Email, pattern, TextSearch.LikeEscape)
                || employee.SearchVector.Matches(EF.Functions.PlainToTsQuery(TextSearch.FullTextConfig, text)));
    }

    private static async Task<EmployeeResponse> SaveAsync(
        Employee employee,
        AppDbContext context,
        IDbContextTransaction transaction,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        response.SetETag(employee.Version);
        return EmployeeResponse.Full(employee);
    }
}
