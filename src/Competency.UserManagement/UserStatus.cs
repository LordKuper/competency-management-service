namespace Competency.UserManagement;

/// <summary>
/// The facts about an account that other modules may rely on.
/// </summary>
/// <param name="Id">The account identifier.</param>
/// <param name="Email">The account's e-mail address.</param>
/// <param name="EmployeeId">The employee the account is bound to; null when it is bound to none.</param>
/// <param name="IsBlocked">Whether an administrator has blocked the account.</param>
/// <param name="IsRegistered">Whether the account has a password, rather than an invitation not yet accepted.</param>
/// <param name="Version">The account's version, which an action on the account sends back in <c>If-Match</c>.</param>
public sealed record UserStatus(Guid Id, string Email, Guid? EmployeeId, bool IsBlocked, bool IsRegistered, int Version)
{
    /// <summary>
    /// Whether the account can act at all: neither blocked nor still invited.
    /// </summary>
    public bool IsActive => !IsBlocked && IsRegistered;
}
