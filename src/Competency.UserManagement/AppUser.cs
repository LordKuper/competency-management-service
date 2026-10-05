using Competency.Platform;
using Microsoft.AspNetCore.Identity;

namespace Competency.UserManagement;

/// <summary>
/// A local account that signs in with a user name and password.
/// A global administrator has no employee; a user is bound to exactly one employee, and one employee has at most one account.
/// Accounts are never deleted: blocking ends them.
/// The properties Identity maintains (hash, stamps, lockout) are deliberately not audited.
/// </summary>
[Audited]
internal sealed class AppUser : IdentityUser<Guid>, IVersioned
{
    public const int UserNameMaxLength = 64;
    public const int PasswordMaxLength = 128;

    /// <summary>
    /// The sign-in name; unique regardless of letter case.
    /// </summary>
    [Audited]
    public override string? UserName { get; set; }

    [Audited]
    public UserRole Role { get; set; }

    /// <summary>
    /// Whether an administrator has blocked the account; a blocked account cannot sign in and its sessions end.
    /// </summary>
    [Audited]
    public bool IsBlocked { get; set; }

    [Audited]
    public Guid? EmployeeId { get; set; }

    /// <inheritdoc />
    public int Version { get; private set; }

    /// <summary>
    /// Ends a lockout caused by failed sign-ins, so an administrator's block release or password reset lets the user in at once.
    /// </summary>
    public void ClearLockout()
    {
        LockoutEnd = null;
        AccessFailedCount = 0;
    }
}
