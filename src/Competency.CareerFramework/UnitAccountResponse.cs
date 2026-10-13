using Competency.UserManagement;

namespace Competency.CareerFramework;

/// <summary>
/// The account of a unit's head or HR and whether it can act. The version is the account's, sent back in <c>If-Match</c> to act on it.
/// </summary>
/// <param name="Id">The account; null when the person has none.</param>
/// <param name="Email">The account's e-mail; null when the person has no account.</param>
/// <param name="State">Whether the account can act.</param>
/// <param name="Version">The account's version; null when the person has no account.</param>
internal sealed record UnitAccountResponse(Guid? Id, string? Email, UnitAccountState State, int? Version)
{
    /// <summary>
    /// Describes an account. A blocked account counts as blocked even while still invited, because unblocking comes first.
    /// </summary>
    /// <param name="account">The account; null when the person has none.</param>
    /// <returns>The account as the API returns it.</returns>
    public static UnitAccountResponse Of(UserStatus? account) => account switch
    {
        null => new(null, null, UnitAccountState.None, null),
        { IsActive: true } => new(account.Id, account.Email, UnitAccountState.Active, account.Version),
        { IsBlocked: true } => new(account.Id, account.Email, UnitAccountState.Blocked, account.Version),
        _ => new(account.Id, account.Email, UnitAccountState.Invited, account.Version),
    };
}
