using Kalibr.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NpgsqlTypes;

namespace Kalibr.OrgStructure;

/// <summary>
/// The unit API. Any signed-in user reads the tree; only global administrators and HR change it, under the tree lock and with <c>If-Match</c>.
/// A unit's employees are those with any assignment in it, the main or a secondary one.
/// A unit still in use is deactivated rather than deleted, and a deactivated unit stays readable for global administrators and HR;
/// only a unit nothing refers to is deleted.
/// </summary>
internal static class OrgUnitEndpoints
{
    private const string Route = "/api/v1/org-units";
    private const string Tag = "OrgUnits";
    private const string ParentSubject = "Родительское подразделение";
    private const string HeadSubject = "Руководитель";
    private const string ProblemContentType = "application/problem+json";
    private const int NamedBlockersLimit = 5;

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

        var keepers = readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        keepers.MapPost(string.Empty, CreateAsync).WithName("CreateOrgUnit").ProducesETag();
        keepers.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateOrgUnit").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        keepers.MapPost("/{id:guid}/move", MoveAsync).WithName("MoveOrgUnit").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        keepers.MapPost("/{id:guid}/deactivate", DeactivateAsync).WithName("DeactivateOrgUnit").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        keepers.MapPost("/{id:guid}/activate", ActivateAsync).WithName("ActivateOrgUnit").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        keepers.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteOrgUnit")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<UnitDeletionRefusal>(StatusCodes.Status409Conflict, ProblemContentType);
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
        var employeeCounts = context.Set<EmployeePlacement>()
            .Where(placement => employees.Any(employee => employee.Id == placement.EmployeeId))
            .GroupBy(placement => placement.OrgUnitId)
            .Select(group => new { OrgUnitId = group.Key, Count = group.Select(placement => placement.EmployeeId).Distinct().Count() });
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
        var placements = context.WorkingPlacements();
        var total = await placements.Where(placement => subtreeIds.Contains(placement.OrgUnitId)).CountEmployeesAsync(cancellationToken);
        var direct = await placements.Where(placement => placement.OrgUnitId == id).CountEmployeesAsync(cancellationToken);

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

            var activeEmployees = await context.WorkingPlacements()
                .Where(placement => placement.OrgUnitId == id)
                .CountEmployeesAsync(cancellationToken);
            if (activeEmployees > 0)
            {
                return Rejection.Conflict($"Нельзя деактивировать подразделение «{unit.Name}»: в нём есть работающие сотрудники ({activeEmployees}). Сначала переведите их в другое подразделение, снимите их совмещения в нём или отметьте как не работающих.");
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
    /// Deletes an empty unit for good, together with its position list and its responsible HR assignment, in one transaction; the journal keeps
    /// its earlier entries. A unit with child units, with employees having any assignment in it, dismissed ones included, or with career data
    /// referring to it is refused with the list of what keeps it.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        [FromServices] IEnumerable<IUnitReferences> references,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var unit = await context.Set<OrgUnit>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (unit is null)
        {
            return TypedResults.NotFound();
        }

        var blockers = await DeletionBlockersAsync(context, id, cancellationToken);
        foreach (var reference in references)
        {
            blockers.AddRange(await reference.BlockersAsync(id, cancellationToken));
        }

        if (blockers.Count > 0)
        {
            return TypedResults.Problem(new UnitDeletionRefusal
            {
                Status = StatusCodes.Status409Conflict,
                Title = Rejection.ConflictTitle,
                Detail = $"Нельзя удалить подразделение «{unit.Name}»: с ним связаны другие данные.",
                Obstacles = blockers,
            });
        }

        context.RemoveRange(await context.Set<OrgUnitPosition>().Where(entry => entry.OrgUnitId == id).ToListAsync(cancellationToken));
        foreach (var reference in references)
        {
            await reference.ReleaseAsync(id, cancellationToken);
        }

        ifMatch.ApplyTo(context, unit);
        context.Remove(unit);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Describes the child units and the employees that keep a unit from being deleted, one line each, naming the first few.
    /// </summary>
    private static async Task<List<string>> DeletionBlockersAsync(AppDbContext context, Guid unitId, CancellationToken cancellationToken)
    {
        List<string> lines = [];
        var children = context.Set<OrgUnit>().Where(child => child.ParentId == unitId);
        var childCount = await children.CountAsync(cancellationToken);
        if (childCount > 0)
        {
            var names = await children.OrderBy(child => child.Name).Select(child => child.Name).Take(NamedBlockersLimit).ToListAsync(cancellationToken);
            lines.Add($"Дочерние подразделения, в том числе неактивные: {childCount}{NamedList(names, childCount)}");
        }

        var placements = context.Set<EmployeePlacement>().Where(placement => placement.OrgUnitId == unitId);
        var holders = context.Set<Employee>().Where(employee => placements.Any(placement => placement.EmployeeId == employee.Id));
        var holderCount = await holders.CountAsync(cancellationToken);
        if (holderCount > 0)
        {
            var dismissedCount = await holders.CountAsync(employee => !employee.IsActive, cancellationToken);
            var names = await holders.OrderBy(employee => employee.FullName).Select(employee => employee.FullName).Take(NamedBlockersLimit).ToListAsync(cancellationToken);
            lines.Add($"Сотрудники с основным назначением или совмещением в подразделении: {holderCount}, из них не работают: {dismissedCount}{NamedList(names, holderCount)}");
        }

        return lines;
    }

    private static string NamedList(IReadOnlyList<string> names, int total) =>
        $" ({string.Join(", ", names)}{(total > names.Count ? $" и ещё {total - names.Count}" : string.Empty)})";

    /// <summary>
    /// Says why an employee cannot head the unit: they do not exist, do not work, or have no assignment in the unit, the main or a secondary one.
    /// </summary>
    private static async Task<string?> HeadProblemAsync(AppDbContext context, Guid headId, Guid unitId, CancellationToken cancellationToken)
    {
        if (await context.EmployeeProblemAsync(headId, HeadSubject, cancellationToken) is { } problem)
        {
            return problem;
        }

        var isInUnit = await context.Set<EmployeePlacement>().AnyAsync(placement => placement.EmployeeId == headId && placement.OrgUnitId == unitId, cancellationToken);
        return isInUnit ? null : "У сотрудника нет назначения в этом подразделении: руководителем может быть только сотрудник этого подразделения, с основным назначением или совмещением.";
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
