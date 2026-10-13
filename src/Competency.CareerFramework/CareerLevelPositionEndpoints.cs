using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of the links of track levels to positions. Whoever reads a track reads its links; whoever may edit the track links and unlinks
/// positions. A link changes the level's version, so it needs the level's <c>If-Match</c>, and the level comes back with its new version.
/// A level may be linked to an active position its track's unit, a unit below it or a unit above it lists. A level of an active track
/// keeps at least one active position.
/// </summary>
internal static class CareerLevelPositionEndpoints
{
    private const string TrackRoute = "/api/v1/career/tracks";
    private const string LevelRoute = "/api/v1/career/levels";
    private const string PositionField = "positionId";
    private const string UnavailablePositionMessage = "Должность неактивна или недоступна подразделению трека";
    private const string LinkedElsewhereMessage = "Должность уже связана с другим уровнем трека";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var tracks = endpoints.MapCatalogGroup(TrackRoute);
        tracks.MapGet("/{id:guid}/positions", ListLinkedAsync).WithName("ListCareerTrackPositions")
            .ProducesProblem(StatusCodes.Status404NotFound);
        tracks.MapGet("/{id:guid}/available-positions", ListAvailableAsync).WithName("ListCareerTrackAvailablePositions")
            .ProducesProblem(StatusCodes.Status404NotFound);

        var levels = endpoints.MapCatalogGroup(LevelRoute);
        levels.MapPost("/{id:guid}/positions", LinkAsync).WithName("LinkCareerLevelPosition").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        levels.MapDelete("/{id:guid}/positions/{positionId:guid}", UnlinkAsync).WithName("UnlinkCareerLevelPosition").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<PublicationRefusal>(StatusCodes.Status409Conflict, CatalogEndpoints.ProblemContentType);
    }

    private static async Task<Results<Ok<TrackPositionsResponse>, NotFound>> ListLinkedAsync(
        Guid id,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CareerTrack>(id, viewer, cancellationToken) is not { } track)
        {
            return TypedResults.NotFound();
        }

        var levels = await context.Set<CareerLevel>()
            .AsNoTracking()
            .Where(viewer.Sees<CareerLevel>())
            .Where(level => level.TrackId == id)
            .OrderBy(level => level.Position)
            .ToListAsync(cancellationToken);
        var links = (await context.Set<CareerLevelPosition>().AsNoTracking().Where(link => link.TrackId == id).ToListAsync(cancellationToken))
            .ToLookup(link => link.LevelId, link => link.PositionId);
        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        var headcount = Headcount(snapshot, track.OwnerOrgUnitId);
        var canEdit = viewer.CanEdit(track);

        return TypedResults.Ok(new TrackPositionsResponse(canEdit, [.. levels.Select(level => new TrackLevelPositionsResponse(
            level.Id,
            level.Name,
            level.Status,
            level.Version,
            TrackReadiness.HasActivePosition(links[level.Id], snapshot.Positions),
            [.. links[level.Id]
                .Select(snapshot.Positions.GetValueOrDefault)
                .OfType<PositionStatus>()
                .OrderBy(position => position.Name, StringComparer.OrdinalIgnoreCase)
                .Select(position => new LinkedPositionResponse(
                    position.Id,
                    position.Name,
                    position.IsActive,
                    headcount.GetValueOrDefault(position.Id),
                    canEdit && !IsLastActivePosition(track, position.Id, links[level.Id], snapshot.Positions)))]))]));
    }

    /// <summary>
    /// Lists, a page at a time, the positions a level of the track may be linked to that no level of the track is linked to yet.
    /// </summary>
    private static async Task<Results<Ok<PageResponse<AvailablePositionResponse>>, NotFound, ValidationProblem>> ListAvailableAsync(
        Guid id,
        string? q,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = ListRequest.DefaultPageSize)
    {
        var errors = ListRequest.Validate(page, pageSize, q);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CareerTrack>(id, viewer, cancellationToken) is not { } track)
        {
            return TypedResults.NotFound();
        }

        var linked = await context.Set<CareerLevelPosition>().Where(link => link.TrackId == id).Select(link => link.PositionId).ToListAsync(cancellationToken);
        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        var headcount = Headcount(snapshot, track.OwnerOrgUnitId);
        var search = q?.Trim() ?? string.Empty;
        var matching = AvailablePositions(snapshot, track.OwnerOrgUnitId)
            .Where(position => !linked.Contains(position.Id) && position.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(position => position.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(position => position.Id)
            .ToList();
        var items = matching
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(position => new AvailablePositionResponse(position.Id, position.Name, headcount.GetValueOrDefault(position.Id)))
            .ToList();
        return TypedResults.Ok(new PageResponse<AvailablePositionResponse>(items, matching.Count, page, pageSize));
    }

    /// <summary>
    /// Links an available position to the level; linking it again changes nothing.
    /// </summary>
    private static async Task<Results<Ok<CareerLevelResponse>, NotFound, ValidationProblem, ProblemHttpResult>> LinkAsync(
        Guid id,
        LevelPositionRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CareerLevel>(id, viewer, cancellationToken) is not { } level)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanEdit(level))
        {
            return Rejection.Forbidden();
        }

        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        if (AvailablePositions(snapshot, level.Track.OwnerOrgUnitId).All(position => position.Id != request.PositionId))
        {
            return Rejection.Invalid(PositionField, UnavailablePositionMessage);
        }

        var linkedLevelId = await context.Set<CareerLevelPosition>()
            .Where(link => link.TrackId == level.TrackId && link.PositionId == request.PositionId)
            .Select(link => (Guid?)link.LevelId)
            .FirstOrDefaultAsync(cancellationToken);
        if (linkedLevelId is { } otherLevelId && otherLevelId != level.Id)
        {
            return Rejection.Conflict(LinkedElsewhereMessage);
        }

        if (linkedLevelId is null)
        {
            ifMatch.ApplyTo(context, level);
            context.Add(new CareerLevelPosition { LevelId = level.Id, PositionId = request.PositionId, TrackId = level.TrackId });
            context.Entry(level).Property(entry => entry.Version).IsModified = true;
        }

        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(level, CareerLevelEndpoints.Kind, viewer, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Unlinks a position from the level, unless it is the last active position of a level of an active track. That refusal is checked
    /// before <c>If-Match</c>, so of two removals racing for a level's last two active positions the second learns why it cannot proceed.
    /// </summary>
    private static async Task<Results<Ok<CareerLevelResponse>, NotFound, ProblemHttpResult>> UnlinkAsync(
        Guid id,
        Guid positionId,
        IfMatch ifMatch,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CareerLevel>(id, viewer, cancellationToken) is not { } level)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanEdit(level))
        {
            return Rejection.Forbidden();
        }

        var links = await context.Set<CareerLevelPosition>().Where(link => link.LevelId == id).ToListAsync(cancellationToken);
        if (links.FirstOrDefault(link => link.PositionId == positionId) is not { } removed)
        {
            return TypedResults.NotFound();
        }

        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        if (IsLastActivePosition(level.Track, positionId, links.Select(link => link.PositionId), snapshot.Positions))
        {
            return PublicationRefusal.Refuse(
                $"Нельзя снять последнюю действующую должность уровня «{level.Name}» действующего трека",
                [new PublicationBlocker(PublicationEntryKind.Level, level.Id, level.Name, PublicationBlockerReason.NoActivePosition, level.Track.Name)]);
        }

        ifMatch.ApplyTo(context, level);
        context.Remove(removed);
        context.Entry(level).Property(entry => entry.Version).IsModified = true;
        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(level, CareerLevelEndpoints.Kind, viewer, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Whether unlinking the position would leave a level of an active track without an active position, whatever the level's own status.
    /// </summary>
    private static bool IsLastActivePosition(
        CareerTrack track,
        Guid positionId,
        IEnumerable<Guid> levelPositionIds,
        IReadOnlyDictionary<Guid, PositionStatus> positions) =>
        track.Status == CatalogStatus.Active
        && TrackReadiness.HasActivePosition([positionId], positions)
        && !TrackReadiness.HasActivePosition(levelPositionIds.Where(linkedId => linkedId != positionId), positions);

    /// <summary>
    /// The active positions a level of a track of the unit may be linked to: those the unit or a unit above it lists, which its employees
    /// may hold, and those a unit below it lists, because a matrix of such a unit may be built on the track.
    /// </summary>
    public static IEnumerable<PositionStatus> AvailablePositions(OrgSnapshot snapshot, Guid unitId) =>
        snapshot.Subtree(unitId)
            .SelectMany(unit => snapshot.UnitPositionIds[unit.Id])
            .Concat(snapshot.AvailablePositionIds(unitId))
            .Distinct()
            .Select(snapshot.Positions.GetValueOrDefault)
            .OfType<PositionStatus>()
            .Where(position => position.IsActive);

    /// <summary>
    /// The working employees of the unit and the units below it, by position.
    /// </summary>
    private static Dictionary<Guid, int> Headcount(OrgSnapshot snapshot, Guid unitId)
    {
        var unitIds = snapshot.Subtree(unitId).Select(unit => unit.Id).ToHashSet();
        return snapshot.WorkingEmployeeCounts
            .Where(count => unitIds.Contains(count.Key.OrgUnitId))
            .GroupBy(count => count.Key.PositionId, count => count.Value)
            .ToDictionary(group => group.Key, group => group.Sum());
    }
}
