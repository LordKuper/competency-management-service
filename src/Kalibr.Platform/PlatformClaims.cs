namespace Kalibr.Platform;

/// <summary>
/// The claim types and values the authorization policies and the current actor rely on; sign-in must issue exactly these.
/// </summary>
public static class PlatformClaims
{
    /// <summary>
    /// The claim holding the identifier of the user account.
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// The claim holding one role of the user account; an account holds one such claim per role, and none when it has no role.
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// The value of the <see cref="Role"/> claim that grants the global administrator policy.
    /// </summary>
    public const string GlobalAdminRole = "GlobalAdmin";

    /// <summary>
    /// The value of the <see cref="Role"/> claim of an account with the HR role.
    /// </summary>
    public const string HrRole = "Hr";

    /// <summary>
    /// The role the journal records for a signed-in account that has no role.
    /// </summary>
    public const string NoRole = "User";

    /// <summary>
    /// The roles of an account as the journal records them, one value for the whole set.
    /// </summary>
    /// <param name="roles">The account's role values.</param>
    /// <returns>The roles separated by commas, or <see cref="NoRole"/> when there are none.</returns>
    public static string JournalRole(IEnumerable<string> roles) => string.Join(",", roles) is { Length: > 0 } list ? list : NoRole;
}
