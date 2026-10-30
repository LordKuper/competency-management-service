namespace Competency.Platform;

/// <summary>
/// Who triggered the current operation and under which request, for auditing.
/// Every property is null and no role is held outside an authenticated request, such as startup or background work.
/// Implementations are safe for concurrent use.
/// </summary>
public interface ICurrentActor
{
    /// <summary>
    /// The identifier of the signed-in user account, from the <see cref="PlatformClaims.Subject"/> claim.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// The roles of the signed-in user account as the journal records them (<see cref="PlatformClaims.JournalRole"/>); not for access checks, which use <see cref="IsInRole"/>.
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Whether the signed-in user account holds the role, by its <see cref="PlatformClaims.Role"/> claims; false outside an authenticated request.
    /// </summary>
    /// <param name="role">The role value, such as <see cref="PlatformClaims.GlobalAdminRole"/>.</param>
    /// <returns><see langword="true"/> when the account has the role.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// The identifier of the current request, also sent to the client as <c>X-Request-Id</c>.
    /// </summary>
    string? RequestId { get; }
}
