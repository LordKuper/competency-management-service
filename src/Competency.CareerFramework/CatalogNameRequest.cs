namespace Competency.CareerFramework;

/// <summary>
/// The name of a catalogue entry, for renaming a direction, track or category; entry requests with more fields extend it.
/// </summary>
internal record CatalogNameRequest
{
    public required string Name { get; init; }

    /// <summary>
    /// The name without the surrounding whitespace, the form in which names are stored and compared; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <returns>The trimmed name.</returns>
    public string TrimmedName() => Name.Trim();

    /// <summary>
    /// Checks the request.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public virtual Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CheckText(errors, "name", Name, CatalogEntry.NameMaxLength, "Укажите название");
        return errors;
    }

    /// <summary>
    /// Checks a text field that is stored trimmed.
    /// </summary>
    /// <param name="errors">The problems found so far, to add to.</param>
    /// <param name="field">The request field.</param>
    /// <param name="text">The field value.</param>
    /// <param name="maxLength">The longest trimmed value allowed.</param>
    /// <param name="missingMessage">The problem of an empty value; null when the field may be empty.</param>
    protected static void CheckText(Dictionary<string, string[]> errors, string field, string? text, int maxLength, string? missingMessage)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            if (missingMessage is not null)
            {
                errors[field] = [missingMessage];
            }
        }
        else if (text.Trim().Length > maxLength)
        {
            errors[field] = [$"Не длиннее {maxLength} символов"];
        }
    }

    /// <summary>
    /// The optional text field as stored: trimmed, and null when empty.
    /// </summary>
    /// <param name="text">The field value.</param>
    /// <returns>The stored value.</returns>
    protected static string? OptionalText(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
