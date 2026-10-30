using Kalibr.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.CareerFramework;

/// <summary>
/// The API of career levels, the parts of a track: whoever may edit the track creates, renames and deletes its levels, and a level is kept by
/// its track's keepers. Level names are unique within their track. Lists give levels in track order. A level no matrix uses can be deleted,
/// and its links to positions go with it.
/// </summary>
internal static class CareerLevelEndpoints
{
    private const string Route = "/api/v1/career/levels";
    private const string TrackField = "trackId";
    private const string InUseMessage = "Уровень используют матрицы — удалить нельзя";

    internal static readonly CatalogKind<CareerLevel, CareerLevelResponse> Kind = new(
        "CareerLevel",
        (level, _, viewer) => CareerLevelResponse.From(level, viewer),
        level => other => other.TrackId == level.TrackId);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListCareerLevels");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateCareerLevel").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapEntryEndpoints(Kind, Route, mapRename: true, mapPublication: false);
        group.MapDeleteEntry(Kind, RemoveLinksAsync);
    }

    private static async Task<string?> RemoveLinksAsync(
        CareerLevel level,
        DeletionBlockers blockers,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        await blockers.AddMatricesAsync(store.MatricesUsing(level));
        if (blockers.Any)
        {
            return InUseMessage;
        }

        var id = level.Id;
        context.RemoveRange(await context.Set<CareerLevelPosition>().Where(link => link.LevelId == id).ToListAsync(cancellationToken));
        return null;
    }

    private static Task<Results<Ok<PageResponse<CareerLevelResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        [FromQuery(Name = TrackField)] Guid? trackId,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken) =>
        CatalogEndpoints.ListAsync(
            context.Set<CareerLevel>().WhereIf(trackId is not null, level => level.TrackId == trackId),
            query,
            Kind,
            store,
            cancellationToken,
            levels => levels.OrderBy(level => level.Track.Name).ThenBy(level => level.TrackId).ThenBy(level => level.Position));

    /// <summary>
    /// Adds a level after the track's other levels to an active or draft track; the level takes the track's status, so a draft track's
    /// levels are approved with it.
    /// </summary>
    private static async Task<Results<Created<CareerLevelResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        NewCareerLevelRequest request,
        AppDbContext context,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CareerTrack>(request.TrackId, viewer, cancellationToken) is not { } track
            || track.Status == CatalogStatus.Inactive)
        {
            return Rejection.Invalid(TrackField, CatalogEndpoints.UnavailableMessage);
        }

        if (!viewer.CanEdit(track))
        {
            return Rejection.Forbidden();
        }

        var lastPosition = await context.Set<CareerLevel>()
            .Where(level => level.TrackId == track.Id)
            .MaxAsync(level => (int?)level.Position, cancellationToken);
        var level = new CareerLevel
        {
            Name = request.TrimmedName(),
            Status = track.Status,
            AuthorId = viewer.UserId,
            TrackId = track.Id,
            Position = (lastPosition ?? 0) + 1,
        };
        if (await CatalogEndpoints.NameProblemAsync(level, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(level);
        await context.Entry(level).Reference(added => added.Track).LoadAsync(cancellationToken);
        var created = await CatalogEndpoints.CommitAsync(level, Kind, viewer, store, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{level.Id}", created);
    }
}
