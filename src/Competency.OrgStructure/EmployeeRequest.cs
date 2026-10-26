namespace Competency.OrgStructure;

/// <summary>
/// The fields of an employee, for creating one and for replacing all of an existing one's fields at once,
/// which covers a transfer to another unit and any change of the assignments: the unit, position and legal entities are the main assignment,
/// and <see cref="Assignments"/> is the whole set of secondary ones, so making a secondary assignment the main one swaps the two in a single request.
/// The status is not among them: a new employee works, and it changes only through dismissal and rehiring.
/// Every field but the middle name is required so that omitting one never changes it by accident;
/// an absent or blank middle name means the employee has none.
/// </summary>
internal sealed record EmployeeRequest
{
    public required string LastName { get; init; }

    public required string FirstName { get; init; }

    public string? MiddleName { get; init; }

    public required Guid PositionId { get; init; }

    public required Guid OrgUnitId { get; init; }

    /// <summary>
    /// The legal entities of the main assignment; empty when it has none.
    /// </summary>
    public required IReadOnlyList<Guid> LegalEntityIds { get; init; }

    /// <summary>
    /// The secondary assignments; empty when the employee has none.
    /// </summary>
    public required IReadOnlyList<AssignmentRequest> Assignments { get; init; }

    /// <summary>
    /// Checks the text fields and that the lists are present.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (LegalEntityIds is null)
        {
            errors["legalEntityIds"] = ["Юридические лица: укажите список, возможно пустой."];
        }

        if (Assignments is null)
        {
            errors["assignments"] = ["Совмещения: укажите список, возможно пустой."];
        }
        else
        {
            for (var index = 0; index < Assignments.Count; index++)
            {
                if (Assignments[index]?.LegalEntityIds is null)
                {
                    errors[$"assignments[{index}]"] = ["Совмещение: укажите список юридических лиц, возможно пустой."];
                }
            }
        }

        CheckText(errors, "lastName", "Фамилия", LastName, Employee.NameMaxLength);
        CheckText(errors, "firstName", "Имя", FirstName, Employee.NameMaxLength);
        if (!string.IsNullOrWhiteSpace(MiddleName))
        {
            CheckText(errors, "middleName", "Отчество", MiddleName, Employee.NameMaxLength);
        }

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
