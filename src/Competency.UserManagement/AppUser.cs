using Competency.Platform;
using Microsoft.AspNetCore.Identity;

namespace Competency.UserManagement;

/// <summary>
/// A local account that signs in with an e-mail address and password.
/// A global administrator has no employee; a user may be bound to one employee, and one employee has at most one account.
/// Accounts are never deleted: blocking ends them.
/// The properties Identity maintains (hash, stamps, lockout) are deliberately not audited.
/// </summary>
[Audited]
internal sealed class AppUser : IdentityUser<Guid>, IVersioned
{
    public const int EmailMaxLength = 254;
    public const int PasswordMaxLength = 128;

    /// <summary>
    /// The sign-in name and contact address; unique regardless of letter case. Change it only through <see cref="SetEmail"/>.
    /// </summary>
    [Audited]
    public override string? Email { get; set; }

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
    /// Sets the e-mail and the Identity user name to the same value: the user name is internal, kept so that Identity's own
    /// lookups and its uniqueness check work, and is never entered or shown.
    /// </summary>
    /// <param name="email">The trimmed, valid e-mail address.</param>
    public void SetEmail(string email)
    {
        Email = email;
        UserName = email;
    }

    /// <summary>
    /// Ends every session issued so far, since a session is valid only under the security stamp it was issued with.
    /// It only changes the tracked account: the caller saves it with its other changes, unlike the user manager's stamp update, which saves at once.
    /// </summary>
    public void EndSessions() => SecurityStamp = Guid.NewGuid().ToString();

    /// <summary>
    /// Ends a lockout caused by failed sign-ins, so an administrator's block release or password reset lets the user in at once.
    /// </summary>
    public void ClearLockout()
    {
        LockoutEnd = null;
        AccessFailedCount = 0;
    }
}
