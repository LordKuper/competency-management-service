using System.Diagnostics;
using Kalibr.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.CareerFramework;

/// <summary>
/// The API that lists the matrix versions using a catalogue entry: of the matrices of a direction or track, of the matrices listing a
/// level, a shared block or a competency, directly or through a shared block, and the versions with a competency on a scale. Archived
/// versions are left out. Only the versions the user may read are listed, so a hidden version is neither named nor counted; an entry the user cannot read answers 404.
/// </summary>
internal static class EntryMatrixEndpoints
{
    /// <summary>
    /// Maps the list of the matrix versions using an entry of the catalogue.
    /// </summary>
    /// <param name="group">The catalogue's route group.</param>
    /// <param name="kind">The catalogue.</param>
    public static void MapEntryMatrices<TEntry, TResponse>(this RouteGroupBuilder group, CatalogKind<TEntry, TResponse> kind)
        where TEntry : CatalogEntry => group.MapGet(
            "/{id:guid}/matrices",
            (Guid id, AppDbContext context, CatalogStore store, MatrixRoute route, CancellationToken cancellationToken) =>
                ListAsync<TEntry>(id, context, store, route, cancellationToken))
        .WithName($"List{kind.Name}Matrices")
        .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<List<EntryMatrixResponse>>, NotFound>> ListAsync<TEntry>(
        Guid id,
        AppDbContext context,
        CatalogStore store,
        MatrixRoute route,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
    {
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<TEntry>(id, viewer, cancellationToken) is not { } entry)
        {
            return TypedResults.NotFound();
        }

        var versions = await VersionsUsing(entry, context, store)
            .AsNoTracking()
            .Where(version => version.Status != MatrixVersionStatus.Archived)
            .ToListAsync(cancellationToken);
        var readable = await route.ReadableAsync(viewer, versions, cancellationToken);

        var trackIds = readable.Select(version => version.Matrix.TrackId).OfType<Guid>().Distinct().ToList();
        var trackNames = await context.Set<CareerTrack>()
            .Where(track => trackIds.Contains(track.Id))
            .ToDictionaryAsync(track => track.Id, track => track.Name, cancellationToken);
        var unitNames = await store.UnitNamesAsync(readable.Select(version => (Guid?)version.Matrix.OrgUnitId), cancellationToken);
        var positionNames = await store.PositionNamesAsync(readable.Select(version => version.Matrix.PositionId), cancellationToken);
        return TypedResults.Ok(readable
            .Select(version => new EntryMatrixResponse(
                version.MatrixId,
                version.Matrix.OrgUnitId,
                unitNames.GetValueOrDefault(version.Matrix.OrgUnitId, string.Empty),
                version.Matrix.TrackId,
                version.Matrix.TrackId is { } trackId ? trackNames.GetValueOrDefault(trackId, string.Empty) : null,
                version.Id,
                version.Number,
                version.Status,
                version.Matrix.PositionId,
                version.Matrix.PositionId is { } positionId ? positionNames.GetValueOrDefault(positionId, string.Empty) : null))
            .OrderBy(row => row.TrackName ?? row.PositionName, TextCollation.Comparer)
            .ThenBy(row => row.OrgUnitName, TextCollation.Comparer)
            .ThenBy(row => row.MatrixId)
            .ThenByDescending(row => row.Number)
            .ToList());
    }

    private static IQueryable<MatrixVersion> VersionsUsing(CatalogEntry entry, AppDbContext context, CatalogStore store)
    {
        var id = entry.Id;
        var versions = context.Set<MatrixVersion>();
        return entry switch
        {
            CareerDirection => versions.Where(version => version.Matrix.DirectionId == id),
            CareerTrack => versions.Where(version => version.Matrix.TrackId == id),
            CompetencyScale scale => store.VersionsOn(scale),
            CareerLevel or CareerCompetency or CompetencyBlock => InMatrices(versions, store.MatricesUsing(entry)),
            _ => throw new UnreachableException($"No matrix versions use entries of type {entry.GetType().Name}."),
        };
    }

    private static IQueryable<MatrixVersion> InMatrices(IQueryable<MatrixVersion> versions, IQueryable<CompetencyMatrix> matrices)
    {
        var matrixIds = matrices.Select(matrix => matrix.Id);
        return versions.Where(version => matrixIds.Contains(version.MatrixId));
    }
}
