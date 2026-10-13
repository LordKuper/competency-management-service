using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// The API of the active catalogue entries nothing uses, for HR and global administrators, who keep the entries of no unit.
/// </summary>
internal static class UnusedEntryEndpoints
{
    private const string Route = "/api/v1/career/unused-entries";

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapCatalogGroup(Route)
        .MapGet(string.Empty, ListAsync)
        .WithName("ListUnusedCatalogEntries")
        .ProducesProblem(StatusCodes.Status403Forbidden);

    /// <summary>
    /// One page of the unused entries, optionally of one kind. The whole list is worked out and then paged, because it stays short.
    /// </summary>
    private static async Task<Results<Ok<PageResponse<UnusedEntryResponse>>, ValidationProblem, ProblemHttpResult>> ListAsync(
        [FromQuery(Name = "kind")] CatalogEntryKind? kind,
        CatalogStore store,
        CancellationToken cancellationToken,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = ListRequest.DefaultPageSize)
    {
        var errors = ListRequest.Validate(page, pageSize);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var viewer = await store.ViewerAsync(cancellationToken);
        if (!viewer.IsAdmin && !viewer.IsHr)
        {
            return Rejection.Forbidden();
        }

        var entries = (await store.UnusedEntriesAsync(cancellationToken))
            .Select(entry => new UnusedEntryResponse(CatalogDraftEndpoints.KindOf(entry), entry.Id, entry.Name, entry.Version))
            .Where(entry => kind is null || entry.Kind == kind)
            .ToList();
        return TypedResults.Ok(new PageResponse<UnusedEntryResponse>(
            [.. entries.Skip((page - 1) * pageSize).Take(pageSize)],
            entries.Count,
            page,
            pageSize));
    }
}
