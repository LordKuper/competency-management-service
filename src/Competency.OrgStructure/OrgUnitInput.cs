namespace Competency.OrgStructure;

/// <summary>
/// The checks a unit's own fields must pass, whichever request carries them.
/// </summary>
internal static class OrgUnitInput
{
    /// <summary>
    /// Checks the name and the validity period.
    /// </summary>
    /// <param name="name">The requested name.</param>
    /// <param name="validFrom">The first day of validity, if any.</param>
    /// <param name="validTo">The last day of validity, if any.</param>
    /// <returns>The problems found by field name; empty when the input is valid.</returns>
    public static Dictionary<string, string[]> Validate(string? name, DateOnly? validFrom, DateOnly? validTo)
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

        if (validFrom > validTo)
        {
            errors["validTo"] = ["Дата окончания не может быть раньше даты начала."];
        }

        return errors;
    }
}
