namespace Competency.UserManagement;

/// <summary>
/// The signed-in account as the interface needs it to adapt to the role.
/// </summary>
internal sealed record CurrentUserResponse(Guid Id, string Email, UserRole Role, Guid? EmployeeId, string? EmployeeName)
{
    public static CurrentUserResponse From(AppUser user, string? employeeName) =>
        new(user.Id, user.Email!, user.Role, user.EmployeeId, employeeName);
}
