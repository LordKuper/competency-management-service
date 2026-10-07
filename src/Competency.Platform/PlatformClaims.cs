namespace Competency.Platform;

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
    /// The claim holding the role of the user account.
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// The value of the <see cref="Role"/> claim that grants the global administrator policy.
    /// </summary>
    public const string GlobalAdminRole = "GlobalAdmin";
}
