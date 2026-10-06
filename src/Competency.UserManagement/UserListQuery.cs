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
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = UserListQuery.DefaultPageSize)
{
    public const string LikeEscape = "\\";
    public const int DefaultPageSize = 50;

    private const int MaxPageSize = 200;
    private const int MaxPage = int.MaxValue / MaxPageSize;
    private const int MaxSearchLength = 200;

    /// <summary>
    /// Checks the paging, the role and the search text.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (Page is < 1 or > MaxPage)
        {
            errors["page"] = [$"page must be between 1 and {MaxPage}."];
        }

        if (PageSize is < 1 or > MaxPageSize)
        {
            errors["pageSize"] = [$"pageSize must be between 1 and {MaxPageSize}."];
        }

        if (Q?.Length > MaxSearchLength)
        {
            errors["q"] = [$"q must not be longer than {MaxSearchLength} characters."];
        }

        if (Role is { } role && !Enum.IsDefined(role))
        {
            errors["role"] = ["role must be a known role."];
        }

        return errors;
    }

    /// <summary>
    /// Builds an <c>ILIKE</c> pattern that matches the search text anywhere in a value, treating the text literally.
    /// </summary>
    /// <returns>The pattern using <see cref="LikeEscape"/> as the escape character, or <see langword="null"/> when there is no text to search for.</returns>
    public string? SearchPattern() => string.IsNullOrWhiteSpace(Q)
        ? null
        : $"%{Q.Trim().Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_")}%";
}
