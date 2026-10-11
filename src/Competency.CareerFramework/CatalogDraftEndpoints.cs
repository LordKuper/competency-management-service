using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of the drafts awaiting the signed-in user's approval: for an HR every draft of no unit and the draft tracks of the units the user
/// is the responsible HR of, and every draft for a global administrator. Anyone else gets an empty list. Draft levels are approved with
/// their track, so the list leaves them out.
/// </summary>
internal static class CatalogDraftEndpoints
{
    private const string Route = "/api/v1/career/drafts";

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapCatalogGroup(Route)
        .MapGet(string.Empty, ListAsync)
        .WithName("ListCatalogDrafts");

    /// <summary>
    /// The drafts, oldest first.
    /// </summary>
    private static async Task<Ok<List<CatalogDraftResponse>>> ListAsync(
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        var viewer = await store.ViewerAsync(cancellationToken);
        if (!viewer.IsAdmin && !viewer.IsHr)
        {
            return TypedResults.Ok(new List<CatalogDraftResponse>());
        }

        var hrUnitIds = viewer.HrUnitIds.ToList();
        List<CatalogEntry> drafts =
        [
            .. await DraftsAsync(context.Set<CareerDirection>(), cancellationToken),
            .. await DraftsAsync(
                context.Set<CareerTrack>().WhereIf(!viewer.IsAdmin, track => hrUnitIds.Contains(track.OwnerOrgUnitId)),
                cancellationToken),
            .. await DraftsAsync(context.Set<CompetencyCategory>(), cancellationToken),
            .. await DraftsAsync(context.Set<CareerCompetency>(), cancellationToken),
            .. await DraftsAsync(context.Set<CompetencyScale>(), cancellationToken),
        ];

        var unitNames = await store.UnitNamesAsync(drafts.Select(draft => draft.UnitId), cancellationToken);
        var authorNames = await store.UserNamesAsync(drafts.Select(draft => draft.AuthorId), cancellationToken);
        return TypedResults.Ok(drafts
            .OrderBy(draft => draft.CreatedAt)
            .ThenBy(draft => draft.Id)
            .Select(draft => new CatalogDraftResponse(
                KindOf(draft),
                draft.Id,
                draft.Name,
                draft.UnitId,
                draft.UnitId is { } unitId ? unitNames.GetValueOrDefault(unitId) : null,
                draft.AuthorId,
                draft.AuthorId is { } authorId ? authorNames.GetValueOrDefault(authorId) : null,
                draft.CreatedAt))
            .ToList());
    }

    private static Task<List<T>> DraftsAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        source.AsNoTracking().Where(entry => entry.Status == CatalogStatus.Draft).ToListAsync(cancellationToken);

    private static CatalogEntryKind KindOf(CatalogEntry entry) => entry switch
    {
        CareerDirection => CatalogEntryKind.Direction,
        CareerTrack => CatalogEntryKind.Track,
        CompetencyCategory => CatalogEntryKind.Category,
        CareerCompetency => CatalogEntryKind.Competency,
        _ => CatalogEntryKind.Scale,
    };
}
