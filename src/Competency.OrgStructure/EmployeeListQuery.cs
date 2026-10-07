using Microsoft.AspNetCore.Mvc;

namespace Competency.OrgStructure;

/// <summary>
/// The filters and paging of the employee list request. Every filter is optional; pages count from 1.
/// Ordinary users only ever see working employees, so for them the status filter can only narrow to the same set or to nothing.
/// </summary>
internal sealed record EmployeeListQuery(
    [FromQuery(Name = "q")] string? Q = null,
    [FromQuery(Name = "isActive")] bool? IsActive = null,
    [FromQuery(Name = "orgUnitId")] Guid? OrgUnitId = null,
    [FromQuery(Name = "includeDescendants")] bool IncludeDescendants = false,
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListQueries.DefaultPageSize)
{
    public Dictionary<string, string[]> Validate() => ListQueries.Validate(Page, PageSize, Q);
}
