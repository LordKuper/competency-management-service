namespace Kalibr.Platform;

/// <summary>
/// Names of the authorization policies registered by the platform, for <c>RequireAuthorization</c>.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Any authenticated user.
    /// </summary>
    public const string Authenticated = "Authenticated";

    /// <summary>
    /// An authenticated user with the <see cref="PlatformClaims.GlobalAdminRole"/> role.
    /// </summary>
    public const string GlobalAdmin = "GlobalAdmin";

    /// <summary>
    /// An authenticated user with the <see cref="PlatformClaims.GlobalAdminRole"/> role, the <see cref="PlatformClaims.HrRole"/> role or both.
    /// </summary>
    public const string GlobalAdminOrHr = "GlobalAdminOrHr";
}
