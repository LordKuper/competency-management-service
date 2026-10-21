using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure;

/// <summary>
/// The API of the positions allowed in a unit. Any signed-in user reads a visible unit's own and inherited positions;
/// only global administrators change the unit's own list, under the tree lock, so an employee is never saved with a position
/// taken off the list at the same moment.
/// </summary>
internal static class OrgUnitPositionEndpoints
{
    private const string Route = "/api/v1/org-units/{id:guid}/positions";
    private const string Tag = "Positions";
    private const string PositionField = "positionId";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var readers = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        readers.MapGet(string.Empty, ListAsync).WithName("ListOrgUnitPositions").ProducesProblem(StatusCodes.Status404NotFound);

        var administrators = readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        administrators.MapPost(string.Empty, AddAsync).WithName("AddOrgUnitPosition");
        administrators.MapDelete("/{positionId:guid}", RemoveAsync).WithName("RemoveOrgUnitPosition");
        administrators.MapGet("/{positionId:guid}/impact", ImpactAsync).WithName("GetOrgUnitPositionImpact");
        administrators.MapPost("/quick-add", QuickAddAsync).WithName("QuickAddOrgUnitPosition").ProducesETag();
    }

    /// <summary>
    /// The positions of the unit's own list and of the lists of every unit above it, each with the unit whose list holds it.
    /// Users other than global administrators and HR get active positions only.
    /// </summary>
    private static async Task<Results<Ok<List<OrgUnitPositionResponse>>, NotFound>> ListAsync(
        Guid id,
        AppDbContext context,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        if (!await context.Set<OrgUnit>().VisibleTo(actor).AnyAsync(unit => unit.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var unitIds = context.AncestorIds(id);
        var positions = await context.Set<OrgUnitPosition>()
            .AsNoTracking()
            .Where(link => unitIds.Contains(link.OrgUnitId))
            .WhereIf(!actor.SeesAll(), link => link.Position.IsActive)
            .OrderBy(link => link.Position.Name)
            .ThenBy(link => link.PositionId)
            .ThenBy(link => link.OrgUnit.Name)
            .Select(link => new OrgUnitPositionResponse(
                link.PositionId,
                link.Position.Name,
                link.Position.IsActive,
                link.OrgUnitId,
                link.OrgUnit.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(positions);
    }

    /// <summary>
    /// Allows an active catalogue position in the unit; allowing one already on the unit's list changes nothing.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ValidationProblem>> AddAsync(
        Guid id,
        OrgUnitPositionRequest request,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        if (!await context.Set<OrgUnit>().AnyAsync(unit => unit.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var isActive = await context.Set<Position>()
            .Where(position => position.Id == request.PositionId)
            .Select(position => (bool?)position.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
        if (isActive is not true)
        {
            return Rejection.Invalid(PositionField, isActive is null ? "Должность не найдена" : "Должность неактивна");
        }

        if (!await context.Set<OrgUnitPosition>().AnyAsync(link => link.OrgUnitId == id && link.PositionId == request.PositionId, cancellationToken))
        {
            context.Add(new OrgUnitPosition { OrgUnitId = id, PositionId = request.PositionId });
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveAsync(
        Guid id,
        Guid positionId,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var link = await context.Set<OrgUnitPosition>()
            .FirstOrDefaultAsync(candidate => candidate.OrgUnitId == id && candidate.PositionId == positionId, cancellationToken);
        if (link is null)
        {
            return TypedResults.NotFound();
        }

        context.Remove(link);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// The number of working employees of the unit and the units below it who hold the position, shown before it is taken
    /// off the unit's list; read-only.
    /// </summary>
    private static async Task<Results<Ok<PositionImpactResponse>, NotFound>> ImpactAsync(
        Guid id,
        Guid positionId,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Set<OrgUnitPosition>().AnyAsync(link => link.OrgUnitId == id && link.PositionId == positionId, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var subtreeIds = context.DescendantIds(id);
        var count = await context.Set<Employee>()
            .CountAsync(
                employee => employee.IsActive && employee.PositionId == positionId && subtreeIds.Contains(employee.OrgUnitId),
                cancellationToken);
        return TypedResults.Ok(new PositionImpactResponse(count));
    }

    /// <summary>
    /// Finds the catalogue position with the name regardless of letter case, or creates it, reactivates it when it is inactive,
    /// and allows it in the unit, all in one transaction, so the employee form can pick it at once.
    /// </summary>
    private static async Task<Results<Ok<PositionResponse>, NotFound, ValidationProblem>> QuickAddAsync(
        Guid id,
        PositionRequest request,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var name = request.TrimmedName();
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        if (!await context.Set<OrgUnit>().AnyAsync(unit => unit.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var position = await PositionEndpoints.FindByNameAsync(context, name, cancellationToken);
        if (position is null)
        {
            position = new Position { Name = name };
            context.Add(position);
        }
        else
        {
            position.IsActive = true;
        }

        if (!await context.Set<OrgUnitPosition>().AnyAsync(link => link.OrgUnitId == id && link.PositionId == position.Id, cancellationToken))
        {
            context.Add(new OrgUnitPosition { OrgUnitId = id, PositionId = position.Id });
        }

        return TypedResults.Ok(await PositionEndpoints.SaveAsync(position, context, transaction, response, cancellationToken));
    }
}
