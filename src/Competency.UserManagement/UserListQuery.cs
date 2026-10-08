using Competency.Platform;
using Microsoft.AspNetCore.Mvc;

namespace Competency.UserManagement;

/// <summary>
/// The filters and paging of the account list request. Every filter is optional; pages count from 1.
/// The search text matches the e-mail anywhere in it, regardless of letter case.
/// </summary>
internal sealed record UserListQuery(
    [FromQuery(Name = "q")] string? Q = null,
    [FromQuery(Name = "role")] UserRole? Role = null,
    [FromQuery(Name = "isBlocked")] bool? IsBlocked = null,
    [FromQuery(Name = "isInvited")] bool? IsInvited = null,
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = ListRequest.DefaultPageSize)
{
    /// <summary>
    /// Checks the paging, the role and the search text.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = ListRequest.Validate(Page, PageSize, Q);
        if (Role is { } role && !Enum.IsDefined(role))
        {
            errors["role"] = ["role must be a known role."];
        }

        return errors;
    }

    /// <summary>
    /// Builds the <c>ILIKE</c> pattern for the search text, ignoring surrounding spaces.
    /// </summary>
    /// <returns>The pattern, or <see langword="null"/> when there is no text to search for.</returns>
    public string? SearchPattern() => string.IsNullOrWhiteSpace(Q) ? null : ListRequest.ContainsPattern(Q.Trim());
}
