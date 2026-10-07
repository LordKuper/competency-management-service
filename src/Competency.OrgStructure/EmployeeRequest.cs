namespace Competency.OrgStructure;

/// <summary>
/// The fields of an employee, for creating one and for replacing all of an existing one's fields at once,
/// which covers a transfer to another unit. The status is not among them: a new employee works, and it changes only through dismissal and rehiring.
/// Every field but the middle name is required so that omitting one never changes it by accident;
/// an absent or blank middle name means the employee has none.
/// </summary>
internal sealed record EmployeeRequest
{
    public required string LastName { get; init; }

    public required string FirstName { get; init; }

    public string? MiddleName { get; init; }

    public required string Position { get; init; }

    public required Guid OrgUnitId { get; init; }

    /// <summary>
    /// Checks the text fields.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CheckText(errors, "lastName", "Фамилия", LastName, Employee.NameMaxLength);
        CheckText(errors, "firstName", "Имя", FirstName, Employee.NameMaxLength);
        if (!string.IsNullOrWhiteSpace(MiddleName))
        {
            CheckText(errors, "middleName", "Отчество", MiddleName, Employee.NameMaxLength);
        }

        CheckText(errors, "position", "Должность", Position, Employee.PositionMaxLength);
        return errors;
    }

    /// <summary>
    /// The same fields without the surrounding whitespace; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <returns>The trimmed request.</returns>
    public EmployeeRequest Trimmed() => this with
    {
        LastName = LastName.Trim(),
        FirstName = FirstName.Trim(),
        MiddleName = string.IsNullOrWhiteSpace(MiddleName) ? null : MiddleName.Trim(),
        Position = Position.Trim(),
    };

    private static void CheckText(Dictionary<string, string[]> errors, string field, string label, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{label}: укажите значение."];
        }
        else if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"{label}: не длиннее {maxLength} символов."];
        }
    }
}
