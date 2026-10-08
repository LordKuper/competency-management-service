using System.Linq.Expressions;

namespace Competency.UserManagement;

/// <summary>
/// An account as administrators see it. Credentials, stamps, lockout state and links are never part of it.
/// <c>isInvited</c> marks an account that has not set its password through its invitation yet and so cannot sign in;
/// <c>mailSent</c> tells whether the mail server accepted the e-mail the request sent to the account, and is null when the request sends none.
/// </summary>
internal sealed record UserResponse(
    Guid Id,
    string Email,
    UserRole Role,
    bool IsBlocked,
    bool IsInvited,
    Guid? EmployeeId,
    string? EmployeeName,
    int Version,
    bool? MailSent)
{
    public static readonly Expression<Func<AppUser, UserResponse>> Projection = user => new UserResponse(
        user.Id,
        user.Email!,
        user.Role,
        user.IsBlocked,
        user.PasswordHash == null,
        user.EmployeeId,
        null,
        user.Version,
        null);

    private static readonly Func<AppUser, UserResponse> Compiled = Projection.Compile();

    public static UserResponse From(AppUser user, string? employeeName) => Compiled(user) with { EmployeeName = employeeName };
}
