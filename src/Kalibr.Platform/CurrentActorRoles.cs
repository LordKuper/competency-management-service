namespace Kalibr.Platform;

/// <summary>
/// The account role checks every module makes on the current actor, so none compares role values itself.
/// </summary>
public static class CurrentActorRoles
{
    /// <summary>
    /// Whether the signed-in account is a global administrator.
    /// </summary>
    /// <param name="actor">The current actor.</param>
    /// <returns><see langword="true"/> when the account has the <see cref="PlatformClaims.GlobalAdminRole"/> role.</returns>
    public static bool IsGlobalAdmin(this ICurrentActor actor) => actor.IsInRole(PlatformClaims.GlobalAdminRole);

    /// <summary>
    /// Whether the signed-in account has the HR role, whatever units it is assigned to.
    /// </summary>
    /// <param name="actor">The current actor.</param>
    /// <returns><see langword="true"/> when the account has the <see cref="PlatformClaims.HrRole"/> role.</returns>
    public static bool IsHr(this ICurrentActor actor) => actor.IsInRole(PlatformClaims.HrRole);
}
