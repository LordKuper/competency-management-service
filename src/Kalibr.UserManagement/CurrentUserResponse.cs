using Kalibr.OrgStructure;

namespace Kalibr.UserManagement;

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
    public static CurrentUserResponse From(AppUser user, IReadOnlyList<UserRole> roles, EmployeeStatus? employee) => new(
        user.Id,
        user.Email!,
        roles,
        user.EmployeeId,
        employee?.FullName,
        employee?.LastName,
        employee?.FirstName,
        employee?.MiddleName);
}
