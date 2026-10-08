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
/// The unit API. Any signed-in user reads the tree; only global administrators change it, under the tree lock and with <c>If-Match</c>.
/// Units are never deleted: they are deactivated, and a deactivated unit stays readable for administrators.
/// </summary>
internal static class OrgUnitEndpoints
{
    private const string Route = "/api/v1/org-units";
    private const string Tag = "OrgUnits";
    private const string ParentSubject = "Родительское подразделение";
    private const string HeadSubject = "Руководитель";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var readers = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        readers.MapGet(string.Empty, ListAsync).WithName("ListOrgUnits");
        readers.MapGet("/tree", TreeAsync).WithName("GetOrgUnitTree");
        readers.MapGet("/{id:guid}", GetAsync).WithName("GetOrgUnit").ProducesETag().ProducesProblem(StatusCodes.Status404NotFound);
        readers.MapGet("/{id:guid}/subtree", SubtreeAsync).WithName("GetOrgUnitSubtree").ProducesProblem(StatusCodes.Status404NotFound);
        readers.MapGet("/{id:guid}/path", PathAsync).WithName("GetOrgUnitPath").ProducesProblem(StatusCodes.Status404NotFound);
        readers.MapGet("/{id:guid}/summary", SummaryAsync).WithName("GetOrgUnitSummary").ProducesProblem(StatusCodes.Status404NotFound);

        var administrators = readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        administrators.MapPost(string.Empty, CreateAsync).WithName("CreateOrgUnit").ProducesETag();
        administrators.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateOrgUnit").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        administrators.MapPost("/{id:guid}/move", MoveAsync).WithName("MoveOrgUnit").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapPost("/{id:guid}/deactivate", DeactivateAsync).WithName("DeactivateOrgUnit").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapPost("/{id:guid}/activate", ActivateAsync).WithName("ActivateOrgUnit").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<PageResponse<OrgUnitResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] OrgUnitListQuery query,
        AppDbContext context,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var text = TextSearch.Normalize(query.Q);
        var pattern = text is null ? null : ListRequest.ContainsPattern(text);
        var page = await context.Set<OrgUnit>()
            .AsNoTracking()
            .VisibleTo(actor)
            .WhereIf(query.IsActive is not null, unit => unit.IsActive == query.IsActive)
            .WhereIf(query.ParentId is not null, unit => unit.ParentId == query.ParentId)
            .WhereIf(
                text is not null,
                unit => EF.Functions.ILike(unit.Name, pattern!, ListRequest.LikeEscape)
                    || unit.SearchVector.Matches(EF.Functions.PlainToTsQuery(TextSearch.FullTextConfig, text!)))
            .OrderBy(unit => unit.Name)
            .ThenBy(unit => unit.Id)
            .Select(OrgUnitResponse.Projection)
            .ToPageAsync(query.Page, query.PageSize, cancellationToken);

        return TypedResults.Ok(page);
    }

    /// <summary>
    /// Every visible unit as a flat list, each with its head's name and employee count in the same query; clients assemble the tree from the parent links.
    /// </summary>
    private static async Task<Ok<List<OrgUnitTreeNodeResponse>>> TreeAsync(
        AppDbContext context,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        var employees = context.Set<Employee>().VisibleTo(actor);
        var employeeCounts = employees
            .GroupBy(employee => employee.OrgUnitId)
            .Select(group => new { OrgUnitId = group.Key, Count = group.Count() });
        var units = await (
            from unit in context.Set<OrgUnit>().AsNoTracking().VisibleTo(actor)
            join employeeCount in employeeCounts on unit.Id equals employeeCount.OrgUnitId into unitCounts
            from employeeCount in unitCounts.DefaultIfEmpty()
            join head in employees on unit.HeadEmployeeId equals head.Id into unitHeads
            from head in unitHeads.DefaultIfEmpty()
            orderby unit.Name, unit.Id
            select new OrgUnitTreeNodeResponse(
                unit.Id,
                unit.Name,
                unit.ParentId,
                unit.HeadEmployeeId,
                unit.IsActive,
                unit.Version,
                head.FullName,
                (int?)employeeCount.Count ?? 0))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(units);
    }

    private static async Task<Results<Ok<OrgUnitResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        ICurrentActor actor,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var unit = await context.Set<OrgUnit>()
            .AsNoTracking()
            .VisibleTo(actor)
            .Where(candidate => candidate.Id == id)
            .Select(OrgUnitResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);
        if (unit is null)
        {
            return TypedResults.NotFound();
        }

        response.SetETag(unit.Version);
        return TypedResults.Ok(unit);
    }

    /// <summary>
    /// The unit and all units below it as a flat list; clients assemble the subtree from the parent links.
    /// </summary>
    private static async Task<Results<Ok<List<OrgUnitResponse>>, NotFound>> SubtreeAsync(
        Guid id,
        AppDbContext context,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        var subtreeIds = context.DescendantIds(id);
        var units = await context.Set<OrgUnit>()
            .AsNoTracking()
            .VisibleTo(actor)
            .Where(unit => subtreeIds.Contains(unit.Id))
            .OrderBy(unit => unit.Name)
            .ThenBy(unit => unit.Id)
            .Select(OrgUnitResponse.Projection)
            .ToListAsync(cancellationToken);

        return units.Any(unit => unit.Id == id) ? TypedResults.Ok(units) : TypedResults.NotFound();
    }

    /// <summary>
    /// The chain of units from the root down to the requested unit, which comes last.
    /// </summary>
    private static async Task<Results<Ok<List<OrgUnitResponse>>, NotFound>> PathAsync(
        Guid id,
        AppDbContext context,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        var ancestorIds = context.AncestorIds(id);
        var ancestry = await context.Set<OrgUnit>()
            .AsNoTracking()
            .VisibleTo(actor)
            .Where(unit => ancestorIds.Contains(unit.Id))
            .Select(OrgUnitResponse.Projection)
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        if (!ancestry.TryGetValue(id, out var current))
        {
            return TypedResults.NotFound();
        }

        var path = new List<OrgUnitResponse> { current };
        while (path.Count < ancestry.Count && current.ParentId is { } parentId && ancestry.TryGetValue(parentId, out current))
        {
            path.Add(current);
        }

        path.Reverse();
        return TypedResults.Ok(path);
    }

    private static async Task<Results<Ok<OrgUnitSummaryResponse>, NotFound>> SummaryAsync(
        Guid id,
        AppDbContext context,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        var isVisible = await context.Set<OrgUnit>().VisibleTo(actor).AnyAsync(unit => unit.Id == id, cancellationToken);
        if (!isVisible)
        {
            return TypedResults.NotFound();
        }

        var subtreeIds = context.DescendantIds(id);
        var employees = context.Set<Employee>().Where(employee => employee.IsActive);
        var total = await employees.CountAsync(employee => subtreeIds.Contains(employee.OrgUnitId), cancellationToken);
        var direct = await employees.CountAsync(employee => employee.OrgUnitId == id, cancellationToken);

        return TypedResults.Ok(new OrgUnitSummaryResponse(id, total, direct));
    }

    private static async Task<Results<Created<OrgUnitResponse>, ValidationProblem>> CreateAsync(
        CreateOrgUnitRequest request,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        if (request.ParentId is { } parentId
            && await context.UnitProblemAsync(parentId, ParentSubject, cancellationToken) is { } parentProblem)
        {
            return Rejection.Invalid("parentId", parentProblem);
        }

        var unit = new OrgUnit
        {
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
        };
        context.Add(unit);

        var created = await SaveAsync(unit, context, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{created.Id}", created);
    }

    private static async Task<Results<Ok<OrgUnitResponse>, NotFound, ValidationProblem>> UpdateAsync(
        Guid id,
        UpdateOrgUnitRequest request,
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

        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var unit = await context.Set<OrgUnit>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (unit is null)
        {
            return TypedResults.NotFound();
        }

        if (request.HeadEmployeeId is { } headId
            && headId != unit.HeadEmployeeId
            && await HeadProblemAsync(context, headId, unit.Id, cancellationToken) is { } headProblem)
        {
            return Rejection.Invalid("headEmployeeId", headProblem);
        }

        ifMatch.ApplyTo(context, unit);
        unit.Name = request.Name.Trim();
        unit.HeadEmployeeId = request.HeadEmployeeId;

        return TypedResults.Ok(await SaveAsync(unit, context, transaction, response, cancellationToken));
    }

    private static async Task<Results<Ok<OrgUnitResponse>, NotFound, ValidationProblem, ProblemHttpResult>> MoveAsync(
        Guid id,
        MoveOrgUnitRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var unit = await context.Set<OrgUnit>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (unit is null)
        {
            return TypedResults.NotFound();
        }

        if (request.ParentId is { } parentId && parentId != unit.ParentId)
        {
            if (await context.UnitProblemAsync(parentId, ParentSubject, cancellationToken) is { } parentProblem)
            {
                return Rejection.Invalid("parentId", parentProblem);
            }

            if (await context.DescendantIds(id).AnyAsync(candidate => candidate == parentId, cancellationToken))
            {
                return Rejection.Conflict($"Нельзя перенести подразделение «{unit.Name}» в себя или в своё дочернее подразделение: иерархия не допускает циклов.");
            }
        }

        ifMatch.ApplyTo(context, unit);
        unit.ParentId = request.ParentId;

        return TypedResults.Ok(await SaveAsync(unit, context, transaction, response, cancellationToken));
    }

    private static async Task<Results<Ok<OrgUnitResponse>, NotFound, ProblemHttpResult>> DeactivateAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var unit = await context.Set<OrgUnit>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (unit is null)
        {
            return TypedResults.NotFound();
        }

        if (unit.IsActive)
        {
            var activeChildren = await context.Set<OrgUnit>()
                .CountAsync(child => child.ParentId == id && child.IsActive, cancellationToken);
            if (activeChildren > 0)
            {
                return Rejection.Conflict($"Нельзя деактивировать подразделение «{unit.Name}»: в нём есть активные дочерние подразделения ({activeChildren}). Сначала деактивируйте или перенесите их.");
            }

            var activeEmployees = await context.Set<Employee>()
                .CountAsync(employee => employee.OrgUnitId == id && employee.IsActive, cancellationToken);
            if (activeEmployees > 0)
            {
                return Rejection.Conflict($"Нельзя деактивировать подразделение «{unit.Name}»: в нём есть работающие сотрудники ({activeEmployees}). Сначала переведите их в другое подразделение или отметьте как не работающих.");
            }
        }

        ifMatch.ApplyTo(context, unit);
        unit.IsActive = false;

        return TypedResults.Ok(await SaveAsync(unit, context, transaction, response, cancellationToken));
    }

    private static async Task<Results<Ok<OrgUnitResponse>, NotFound, ProblemHttpResult>> ActivateAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var unit = await context.Set<OrgUnit>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (unit is null)
        {
            return TypedResults.NotFound();
        }

        if (!unit.IsActive)
        {
            if (unit.ParentId is { } parentId
                && await context.UnitProblemAsync(parentId, ParentSubject, cancellationToken) is { } parentProblem)
            {
                return Rejection.Conflict($"Нельзя активировать подразделение «{unit.Name}». {parentProblem}");
            }

            if (unit.HeadEmployeeId is { } headId
                && await context.EmployeeProblemAsync(headId, HeadSubject, cancellationToken) is { } headProblem)
            {
                return Rejection.Conflict($"Нельзя активировать подразделение «{unit.Name}». {headProblem} Назначьте другого руководителя.");
            }
        }

        ifMatch.ApplyTo(context, unit);
        unit.IsActive = true;

        return TypedResults.Ok(await SaveAsync(unit, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Says why an employee cannot head the unit: they do not exist, do not work, or work in another unit.
    /// </summary>
    private static async Task<string?> HeadProblemAsync(AppDbContext context, Guid headId, Guid unitId, CancellationToken cancellationToken)
    {
        if (await context.EmployeeProblemAsync(headId, HeadSubject, cancellationToken) is { } problem)
        {
            return problem;
        }

        var isInUnit = await context.Set<Employee>().AnyAsync(employee => employee.Id == headId && employee.OrgUnitId == unitId, cancellationToken);
        return isInUnit ? null : "Сотрудник работает в другом подразделении: руководителем может быть только сотрудник этого подразделения.";
    }

    private static async Task<OrgUnitResponse> SaveAsync(
        OrgUnit unit,
        AppDbContext context,
        IDbContextTransaction transaction,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        response.SetETag(unit.Version);
        return OrgUnitResponse.From(unit);
    }
}
