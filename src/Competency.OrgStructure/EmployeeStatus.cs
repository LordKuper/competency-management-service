namespace Competency.OrgStructure;

/// <summary>
/// The facts about an employee that other modules may rely on.
/// </summary>
/// <param name="Id">The employee identifier.</param>
/// <param name="FullName">The employee's full name.</param>
/// <param name="LastName">The employee's last name.</param>
/// <param name="FirstName">The employee's first name.</param>
/// <param name="MiddleName">The employee's patronymic; null when the person has none or it is not known.</param>
/// <param name="IsActive">Whether the employee works in the organization.</param>
public sealed record EmployeeStatus(Guid Id, string FullName, string LastName, string FirstName, string? MiddleName, bool IsActive);
