namespace Competency.OrgStructure;

/// <summary>
/// The conventions shared by the search indexes and the search queries: the Russian full-text configuration and substring matching.
/// </summary>
internal static class TextSearch
{
    public const string FullTextConfig = "russian";
    public const string TrigramOperators = "gin_trgm_ops";
    public const int MaxLength = 200;
    public const string LikeEscape = "\\";

    /// <summary>
    /// Prepares a user's search text.
    /// </summary>
    /// <param name="text">The text as typed, possibly absent.</param>
    /// <returns>The trimmed text, or <see langword="null"/> when there is nothing to search for.</returns>
    public static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>
    /// Builds an <c>ILIKE</c> pattern that matches the text anywhere in a value, treating the text literally.
    /// </summary>
    /// <param name="text">The text to find.</param>
    /// <returns>The pattern, using <see cref="LikeEscape"/> as the escape character.</returns>
    public static string ContainsPattern(string text) =>
        $"%{text.Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_")}%";
}
