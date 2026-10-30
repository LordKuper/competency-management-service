using Competency.Platform;
using Microsoft.AspNetCore.Mvc;

namespace Competency.OrgStructure;

/// <summary>
/// The search text and paging of the legal entity list request; pages count from 1.
/// </summary>
internal sealed record LegalEntityListQuery(
    [FromQuery(Name = "q")] string? Q = null,
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListRequest.DefaultPageSize)
{
    public Dictionary<string, string[]> Validate() => ListRequest.Validate(Page, PageSize, Q);
}
