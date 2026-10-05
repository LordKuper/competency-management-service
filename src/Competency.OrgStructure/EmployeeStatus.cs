namespace Competency.OrgStructure;

/// <summary>
/// The facts about an employee that other modules may rely on.
/// </summary>
/// <param name="Id">The employee identifier.</param>
/// <param name="FullName">The employee's full name.</param>
/// <param name="IsActive">Whether the employee works in the organization.</param>
public sealed record EmployeeStatus(Guid Id, string FullName, bool IsActive);
