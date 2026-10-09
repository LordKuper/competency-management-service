using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of the drafts awaiting the signed-in user's approval: those of the units the user is the responsible HR of, and every draft
/// for a global administrator. Anyone else gets an empty list.
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
        if (!viewer.IsAdmin && viewer.HrUnitIds.Count == 0)
        {
            return TypedResults.Ok(new List<CatalogDraftResponse>());
        }

        List<DraftRow> drafts =
        [
            .. await DraftsAsync<CareerDirection>(context, viewer, CatalogEntryKind.Direction, cancellationToken),
            .. await DraftsAsync<CareerTrack>(context, viewer, CatalogEntryKind.Track, cancellationToken),
            .. await DraftsAsync<CareerLevel>(context, viewer, CatalogEntryKind.Level, cancellationToken),
            .. await DraftsAsync<CompetencyCategory>(context, viewer, CatalogEntryKind.Category, cancellationToken),
            .. await DraftsAsync<CareerCompetency>(context, viewer, CatalogEntryKind.Competency, cancellationToken),
            .. await DraftsAsync<CompetencyScale>(context, viewer, CatalogEntryKind.Scale, cancellationToken),
        ];

        var unitNames = await store.UnitNamesAsync(drafts.Select(draft => draft.OwnerOrgUnitId), cancellationToken);
        var authorNames = await store.UserNamesAsync(drafts.Select(draft => draft.AuthorId), cancellationToken);
        return TypedResults.Ok(drafts
            .OrderBy(draft => draft.CreatedAt)
            .ThenBy(draft => draft.Id)
            .Select(draft => new CatalogDraftResponse(
                draft.Kind,
                draft.Id,
                draft.Name,
                draft.OwnerOrgUnitId,
                draft.OwnerOrgUnitId is { } unitId ? unitNames.GetValueOrDefault(unitId) : null,
                draft.AuthorId,
                draft.AuthorId is { } authorId ? authorNames.GetValueOrDefault(authorId) : null,
                draft.CreatedAt))
            .ToList());
    }

    private static Task<List<DraftRow>> DraftsAsync<T>(
        AppDbContext context,
        CatalogViewer viewer,
        CatalogEntryKind kind,
        CancellationToken cancellationToken)
        where T : CatalogEntry
    {
        var unitIds = viewer.HrUnitIds.Select(id => (Guid?)id).ToList();
        return context.Set<T>()
            .AsNoTracking()
            .Where(entry => entry.Status == CatalogStatus.Draft)
            .WhereIf(!viewer.IsAdmin, entry => unitIds.Contains(entry.OwnerOrgUnitId))
            .Select(entry => new DraftRow(kind, entry.Id, entry.Name, entry.OwnerOrgUnitId, entry.AuthorId, entry.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private sealed record DraftRow(CatalogEntryKind Kind, Guid Id, string Name, Guid? OwnerOrgUnitId, Guid? AuthorId, DateTimeOffset CreatedAt);
}
