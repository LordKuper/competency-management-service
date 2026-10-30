using Competency.Platform;
using Microsoft.AspNetCore.Mvc;

namespace Competency.Audit;

/// <summary>
/// The filters and paging of the journal list request. Every filter is optional and an empty value means no filter;
/// the period bounds are inclusive and pages count from 1.
/// </summary>
internal sealed record AuditQuery(
    [FromQuery(Name = "from")] DateTimeOffset? From = null,
    [FromQuery(Name = "to")] DateTimeOffset? To = null,
    [FromQuery(Name = "actor")] string? Actor = null,
    [FromQuery(Name = "action")] string? Action = null,
    [FromQuery(Name = "entityType")] string? EntityType = null,
    [FromQuery(Name = "entityId")] string? EntityId = null,
    [FromQuery(Name = "requestId")] string? RequestId = null,
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListRequest.DefaultPageSize)
{
    /// <summary>
    /// Checks the request against the paging limits and the period order.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = ListRequest.Validate(Page, PageSize);
        if (From > To)
        {
            errors["to"] = ["to must not be earlier than from."];
        }

        return errors;
    }
}
