using Microsoft.AspNetCore.Mvc;

namespace Competency.OrgStructure;

/// <summary>
/// The filters and paging of the unit list request. Every filter is optional; pages count from 1.
/// Ordinary users only ever see active units, so for them the status filter can only narrow to the same set or to nothing.
/// </summary>
internal sealed record OrgUnitListQuery(
    [FromQuery(Name = "q")] string? Q = null,
    [FromQuery(Name = "isActive")] bool? IsActive = null,
    [FromQuery(Name = "parentId")] Guid? ParentId = null,
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListQueries.DefaultPageSize)
{
    public Dictionary<string, string[]> Validate() => ListQueries.Validate(Page, PageSize, Q);
}
