using Competency.Platform;
using Microsoft.AspNetCore.Mvc;

namespace Competency.Promotion;

/// <summary>
/// The paging and filters of the list of the applications of a wave; pages count from 1. The search finds a part of the candidate's name.
/// </summary>
internal sealed record ApplicationListQuery(
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListRequest.DefaultPageSize,
    [FromQuery(Name = "status")] ApplicationStatus? Status = null,
    [FromQuery(Name = "needsFormalData")] bool? NeedsFormalData = null,
    [FromQuery(Name = "search")] string? Search = null)
{
    public Dictionary<string, string[]> Validate() => ListRequest.Validate(Page, PageSize, Search);
}
