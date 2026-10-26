namespace Competency.OrgStructure.Import;

/// <summary>
/// The text conventions of the import: how a cell is cleaned and how two names are compared.
/// </summary>
internal static class ImportText
{
    /// <summary>
    /// Trims the value and collapses every run of whitespace into one space, the form in which names are stored.
    /// </summary>
    /// <param name="value">The raw cell text, possibly absent.</param>
    /// <returns>The cleaned text; empty when there is nothing but whitespace.</returns>
    public static string Clean(string? value) =>
        value is null ? string.Empty : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The comparison key of a cleaned name: lower case, with the letter «ё» equal to «е».
    /// </summary>
    /// <param name="cleaned">The name as <see cref="Clean"/> returned it.</param>
    /// <returns>The key.</returns>
    public static string Key(string cleaned) => cleaned.ToLowerInvariant().Replace('ё', 'е');
}
