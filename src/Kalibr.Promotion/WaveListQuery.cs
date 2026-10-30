using Kalibr.Platform;
using Microsoft.AspNetCore.Mvc;

namespace Kalibr.Promotion;

/// <summary>
/// The paging of the wave list request; pages count from 1.
/// </summary>
internal sealed record WaveListQuery(
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListRequest.DefaultPageSize)
{
    public Dictionary<string, string[]> Validate() => ListRequest.Validate(Page, PageSize);
}
