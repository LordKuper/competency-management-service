namespace Competency.Platform;

/// <summary>
/// The paging limits and search-text conventions that every list request shares.
/// </summary>
public static class ListRequest
{
    /// <summary>
    /// The number of rows per page when the request names none.
    /// </summary>
    public const int DefaultPageSize = 50;

    /// <summary>
    /// The longest search text a request may carry.
    /// </summary>
    public const int MaxSearchLength = 200;

    /// <summary>
    /// The escape character of <see cref="ContainsPattern"/>.
    /// </summary>
    public const string LikeEscape = "\\";

    private const int MaxPageSize = 200;
    private const int MaxPage = int.MaxValue / MaxPageSize;

    /// <summary>
    /// Checks the paging and the search text of a list request.
    /// </summary>
    /// <param name="page">The requested page, counting from 1.</param>
    /// <param name="pageSize">The requested number of rows per page.</param>
    /// <param name="searchText">The requested search text, if the list is searchable.</param>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public static Dictionary<string, string[]> Validate(int page, int pageSize, string? searchText = null)
    {
        var errors = new Dictionary<string, string[]>();
        if (page is < 1 or > MaxPage)
        {
            errors["page"] = [$"page must be between 1 and {MaxPage}."];
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            errors["pageSize"] = [$"pageSize must be between 1 and {MaxPageSize}."];
        }

        if (searchText?.Length > MaxSearchLength)
        {
            errors["q"] = [$"q must not be longer than {MaxSearchLength} characters."];
        }

        return errors;
    }

    /// <summary>
    /// Builds an <c>ILIKE</c> pattern that matches the text anywhere in a value, treating the text literally.
    /// </summary>
    /// <param name="text">The text to find.</param>
    /// <returns>The pattern, using <see cref="LikeEscape"/> as the escape character.</returns>
    public static string ContainsPattern(string text) =>
        $"%{text.Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_")}%";
}
