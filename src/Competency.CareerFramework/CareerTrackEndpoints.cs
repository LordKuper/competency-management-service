using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of career tracks. A track belongs to one unit; track names are unique within their direction. Whoever may edit a track puts its
/// levels in order. Deleting a track deletes its levels.
/// </summary>
internal static class CareerTrackEndpoints
{
    private const string Route = "/api/v1/career/tracks";
    private const string DirectionField = "directionId";
    private const string OrgUnitField = "ownerOrgUnitId";
    private const string ForOrgUnitField = "forOrgUnitId";
    private const string LevelsField = "levelIds";
    private const string InUseMessage = "Трек используют матрицы — удалить нельзя";

    private static readonly CatalogKind<CareerTrack, CareerTrackResponse> Kind = new(
        "CareerTrack",
        CareerTrackResponse.From,
        track => other => other.DirectionId == track.DirectionId);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListCareerTracks");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateCareerTrack").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapEntryEndpoints(Kind, mapRename: true);
        group.MapPut("/{id:guid}/level-order", OrderLevelsAsync).WithName("OrderCareerTrackLevels").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDeleteEntry(Kind, RemoveLevelsAsync);
    }

    /// <summary>
    /// A track goes with its levels, unless a matrix is built on it.
    /// </summary>
    private static async Task<string?> RemoveLevelsAsync(
        CareerTrack track,
        DeletionBlockers blockers,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        var id = track.Id;
        await blockers.AddMatricesAsync(context.Set<CompetencyMatrix>().Where(matrix => matrix.TrackId == id));
        if (blockers.Any)
        {
            return InUseMessage;
        }

        context.RemoveRange(await context.Set<CareerLevel>().Where(level => level.TrackId == id).ToListAsync(cancellationToken));
        return null;
    }

    /// <summary>
    /// Puts the track's levels in the listed order, the most junior first; levels the list leaves out, such as one added meanwhile or one
    /// the user cannot read, follow in their current order. The change is a change of the track, so it takes the track's version.
    /// </summary>
    private static async Task<Results<Ok<CareerTrackResponse>, NotFound, ValidationProblem, ProblemHttpResult>> OrderLevelsAsync(
        Guid id,
        LevelOrderRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CareerTrack>(id, viewer, cancellationToken) is not { } track)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanEdit(track))
        {
            return Rejection.Forbidden();
        }

        var levels = await context.Set<CareerLevel>().Where(level => level.TrackId == id).OrderBy(level => level.Position).ToListAsync(cancellationToken);
        var listed = (request.LevelIds ?? []).Distinct().ToList();
        if (listed.Any(levelId => levels.All(level => level.Id != levelId)))
        {
            return Rejection.Invalid(LevelsField, CatalogEndpoints.UnavailableMessage);
        }

        List<CareerLevel> ordered = [.. listed.Select(levelId => levels.First(level => level.Id == levelId)), .. levels.Where(level => !listed.Contains(level.Id))];
        await store.MoveLevelsAsync(ordered, cancellationToken);
        ifMatch.ApplyTo(context, track);
        context.Entry(track).Property(entry => entry.Version).IsModified = true;
        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(track, Kind, viewer, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Lists the tracks; <c>forOrgUnitId</c> keeps those a matrix of that unit may be built on: kept by the unit or a unit above it.
    /// </summary>
    private static async Task<Results<Ok<PageResponse<CareerTrackResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        [FromQuery(Name = DirectionField)] Guid? directionId,
        [FromQuery(Name = OrgUnitField)] Guid? ownerOrgUnitId,
        [FromQuery(Name = ForOrgUnitField)] Guid? forOrgUnitId,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        var ancestry = forOrgUnitId is { } unitId
            ? (await employees.FindAncestryAsync(unitId, cancellationToken)).Select(unit => unit.Id).ToList()
            : [];
        return await CatalogEndpoints.ListAsync(
            context.Set<CareerTrack>()
                .WhereIf(directionId is not null, track => track.DirectionId == directionId)
                .WhereIf(ownerOrgUnitId is not null, track => track.OwnerOrgUnitId == ownerOrgUnitId)
                .WhereIf(forOrgUnitId is not null, track => ancestry.Contains(track.OwnerOrgUnitId)),
            query,
            Kind,
            store,
            cancellationToken);
    }

    private static async Task<Results<Created<CareerTrackResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        NewCareerTrackRequest request,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        if (await employees.FindUnitAsync(request.OwnerOrgUnitId, cancellationToken) is not { IsActive: true })
        {
            return Rejection.Invalid(OrgUnitField, CatalogEndpoints.UnavailableMessage);
        }

        var viewer = await store.ViewerAsync(cancellationToken);
        if (viewer.StatusOfNew(request.OwnerOrgUnitId) is not { } status)
        {
            return Rejection.Forbidden();
        }

        if (!await store.IsUsableAsync<CareerDirection>(request.DirectionId, viewer.UserId, cancellationToken))
        {
            return Rejection.Invalid(DirectionField, CatalogEndpoints.UnavailableMessage);
        }

        var track = new CareerTrack
        {
            Name = request.TrimmedName(),
            OwnerOrgUnitId = request.OwnerOrgUnitId,
            Status = status,
            AuthorId = viewer.UserId,
            DirectionId = request.DirectionId,
        };
        if (await CatalogEndpoints.NameProblemAsync(track, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(track);
        var created = await CatalogEndpoints.CommitAsync(track, Kind, viewer, store, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{track.Id}", created);
    }
}
