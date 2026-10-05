namespace Competency.Platform;

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
    /// An authenticated user whose role claim is <see cref="PlatformClaims.GlobalAdminRole"/>.
    /// </summary>
    public const string GlobalAdmin = "GlobalAdmin";
}
