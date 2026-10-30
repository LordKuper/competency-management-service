using Competency.Platform;
using Microsoft.AspNetCore.Mvc;

namespace Competency.OrgStructure;

/// <summary>
/// The filters and paging of the employee list request. Every filter is optional; pages count from 1.
/// Users other than global administrators and HR only ever see working employees, so for them the status filter can only narrow to the same set or to nothing.
/// <c>hrCandidate=true</c>, for global administrators only, keeps the working employees whose account can make them a responsible HR,
/// and then the search text also matches the account's e-mail.
/// </summary>
internal sealed record EmployeeListQuery(
    [FromQuery(Name = "q")] string? Q = null,
    [FromQuery(Name = "isActive")] bool? IsActive = null,
    [FromQuery(Name = "orgUnitId")] Guid? OrgUnitId = null,
    [FromQuery(Name = "includeDescendants")] bool IncludeDescendants = false,
    [FromQuery(Name = "hrCandidate")] bool HrCandidate = false,
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListRequest.DefaultPageSize)
{
    public Dictionary<string, string[]> Validate() => ListRequest.Validate(Page, PageSize, Q);
}
