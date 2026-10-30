namespace Competency.OrgStructure;

/// <summary>
/// The conventions shared by the search indexes and the search queries: the Russian full-text configuration and text preparation.
/// </summary>
internal static class TextSearch
{
    public const string FullTextConfig = "russian";
    public const string TrigramOperators = "gin_trgm_ops";

    /// <summary>
    /// Prepares a user's search text.
    /// </summary>
    /// <param name="text">The text as typed, possibly absent.</param>
    /// <returns>The trimmed text, or <see langword="null"/> when there is nothing to search for.</returns>
    public static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
