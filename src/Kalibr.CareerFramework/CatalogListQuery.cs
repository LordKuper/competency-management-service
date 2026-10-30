using Kalibr.Platform;
using Microsoft.AspNetCore.Mvc;

namespace Kalibr.CareerFramework;

/// <summary>
/// The filters and paging every catalogue list shares. Every filter is optional; pages count from 1. The list holds only the entries
/// the user can read, so a status filter can only narrow it. <c>inWork=true</c> leaves out the inactive entries, as a picker offers them.
/// </summary>
internal sealed record CatalogListQuery(
    [FromQuery(Name = "q")] string? Q = null,
    [FromQuery(Name = "status")] CatalogStatus? Status = null,
    [FromQuery(Name = "inWork")] bool? InWork = null,
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListRequest.DefaultPageSize)
{
    public Dictionary<string, string[]> Validate() => ListRequest.Validate(Page, PageSize, Q);

    /// <summary>
    /// The pattern of names that contain the search text, or <see langword="null"/> when the request searches for nothing.
    /// </summary>
    /// <returns>The <c>ILIKE</c> pattern.</returns>
    public string? NamePattern() => string.IsNullOrWhiteSpace(Q) ? null : ListRequest.ContainsPattern(Q.Trim());
}
