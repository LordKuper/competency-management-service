using Competency.OrgStructure;

namespace Competency.UserManagement;

/// <summary>
/// The signed-in account as the interface needs it to adapt to its roles and to name the user.
/// </summary>
internal sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    IReadOnlyList<UserRole> Roles,
    Guid? EmployeeId,
    string? EmployeeName,
    string? EmployeeLastName,
    string? EmployeeFirstName,
    string? EmployeeMiddleName)
{
    public static CurrentUserResponse From(AppUser user, EmployeeStatus? employee) => new(
        user.Id,
        user.Email!,
        user.Roles,
        user.EmployeeId,
        employee?.FullName,
        employee?.LastName,
        employee?.FirstName,
        employee?.MiddleName);
}
