using System.Net.Mail;

namespace Competency.OrgStructure;

/// <summary>
/// The fields of an employee, for creating one and for replacing all of an existing one's fields at once,
/// which covers a transfer to another unit and a change of status. Every field is required so that omitting one never changes it by accident.
/// </summary>
internal sealed record EmployeeRequest
{
    public required string FullName { get; init; }

    public required string PersonnelNumber { get; init; }

    public required string Email { get; init; }

    public required string Position { get; init; }

    public required Guid OrgUnitId { get; init; }

    public required bool IsActive { get; init; }

    /// <summary>
    /// Checks the text fields.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CheckText(errors, "fullName", "ФИО", FullName, Employee.FullNameMaxLength);
        CheckText(errors, "personnelNumber", "Табельный номер", PersonnelNumber, Employee.PersonnelNumberMaxLength);
        CheckText(errors, "position", "Должность", Position, Employee.PositionMaxLength);
        if (CheckText(errors, "email", "E-mail", Email, Employee.EmailMaxLength) && !IsEmailAddress(Email.Trim()))
        {
            errors["email"] = ["Укажите корректный адрес e-mail."];
        }

        return errors;
    }

    /// <summary>
    /// The same fields without the surrounding whitespace; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <returns>The trimmed request.</returns>
    public EmployeeRequest Trimmed() => this with
    {
        FullName = FullName.Trim(),
        PersonnelNumber = PersonnelNumber.Trim(),
        Email = Email.Trim(),
        Position = Position.Trim(),
    };

    private static bool CheckText(Dictionary<string, string[]> errors, string field, string label, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{label}: укажите значение."];
            return false;
        }

        if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"{label}: не длиннее {maxLength} символов."];
            return false;
        }

        return true;
    }

    private static bool IsEmailAddress(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value;
}
