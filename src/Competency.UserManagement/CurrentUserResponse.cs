using Competency.OrgStructure;

namespace Competency.UserManagement;

/// <summary>
/// The signed-in account as the interface needs it to adapt to the role and to name the user.
/// </summary>
internal sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    UserRole Role,
    Guid? EmployeeId,
    string? EmployeeName,
    string? EmployeeLastName,
    string? EmployeeFirstName,
    string? EmployeeMiddleName)
{
    public static CurrentUserResponse From(AppUser user, EmployeeStatus? employee) => new(
        user.Id,
        user.Email!,
        user.Role,
        user.EmployeeId,
        employee?.FullName,
        employee?.LastName,
        employee?.FirstName,
        employee?.MiddleName);
}
