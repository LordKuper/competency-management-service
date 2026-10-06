namespace Competency.OrgStructure;

/// <summary>
/// The checks a unit's own fields must pass, whichever request carries them.
/// </summary>
internal static class OrgUnitInput
{
    /// <summary>
    /// Checks the name.
    /// </summary>
    /// <param name="name">The requested name.</param>
    /// <returns>The problems found by field name; empty when the input is valid.</returns>
    public static Dictionary<string, string[]> Validate(string? name)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Укажите название подразделения."];
        }
        else if (name.Trim().Length > OrgUnit.NameMaxLength)
        {
            errors["name"] = [$"Название не должно быть длиннее {OrgUnit.NameMaxLength} символов."];
        }

        return errors;
    }
}
