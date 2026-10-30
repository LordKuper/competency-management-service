using Kalibr.Platform;
using Microsoft.AspNetCore.Mvc;

namespace Kalibr.OrgStructure;

/// <summary>
/// The filters and paging of the position list request. Every filter is optional; pages count from 1.
/// Ordinary users only ever see active positions, so for them the status filter can only narrow to the same set or to nothing.
/// </summary>
internal sealed record PositionListQuery(
    [FromQuery(Name = "q")] string? Q = null,
    [FromQuery(Name = "isActive")] bool? IsActive = null,
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListRequest.DefaultPageSize)
{
    public Dictionary<string, string[]> Validate() => ListRequest.Validate(Page, PageSize, Q);
}
