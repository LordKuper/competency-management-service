using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API that lists the matrix versions using a catalogue entry: of the matrices of a direction or track, of the matrices listing a
/// level, a competency or a competency of a category, and the versions on a scale. Archived versions are left out. Only the versions the
/// user may read are listed, so a hidden version is neither named nor counted; an entry the user cannot read answers 404.
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

        var trackIds = readable.Select(version => version.Matrix.TrackId).Distinct().ToList();
        var trackNames = await context.Set<CareerTrack>()
            .Where(track => trackIds.Contains(track.Id))
            .ToDictionaryAsync(track => track.Id, track => track.Name, cancellationToken);
        var unitNames = await store.UnitNamesAsync(readable.Select(version => (Guid?)version.Matrix.OrgUnitId), cancellationToken);
        return TypedResults.Ok(readable
            .Select(version => new EntryMatrixResponse(
                version.MatrixId,
                version.Matrix.OrgUnitId,
                unitNames.GetValueOrDefault(version.Matrix.OrgUnitId, string.Empty),
                version.Matrix.TrackId,
                trackNames.GetValueOrDefault(version.Matrix.TrackId, string.Empty),
                version.Id,
                version.Number,
                version.Status))
            .OrderBy(row => row.TrackName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.OrgUnitName, StringComparer.OrdinalIgnoreCase)
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
            CompetencyScale => versions.Where(version => version.ScaleId == id),
            CompetencyCategory category => InMatrices(versions, store.MatricesUsingCategory(category)),
            _ => InMatrices(versions, store.MatricesUsing(entry)),
        };
    }

    private static IQueryable<MatrixVersion> InMatrices(IQueryable<MatrixVersion> versions, IQueryable<CompetencyMatrix> matrices)
    {
        var matrixIds = matrices.Select(matrix => matrix.Id);
        return versions.Where(version => matrixIds.Contains(version.MatrixId));
    }
}
