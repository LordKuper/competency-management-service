namespace Competency.OrgStructure;

/// <summary>
/// The name of a position, for creating one, renaming one, or finding or creating one by name.
/// </summary>
internal sealed record PositionRequest
{
    public required string Name { get; init; }

    /// <summary>
    /// The name without the surrounding whitespace, the form in which names are stored and compared; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <returns>The trimmed name.</returns>
    public string TrimmedName() => Name.Trim();

    /// <summary>
    /// Checks the name.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors["name"] = ["Укажите название"];
        }
        else if (Name.Trim().Length > Position.NameMaxLength)
        {
            errors["name"] = [$"Не длиннее {Position.NameMaxLength} символов"];
        }

        return errors;
    }
}
