namespace Competency.Platform;

/// <summary>
/// Who triggered the current operation and under which request, for auditing.
/// Every member is null outside an authenticated request, such as startup or background work.
/// Implementations are safe for concurrent use.
/// </summary>
public interface ICurrentActor
{
    /// <summary>
    /// The identifier of the signed-in user account, from the <see cref="PlatformClaims.Subject"/> claim.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// The role of the signed-in user account, from the <see cref="PlatformClaims.Role"/> claim.
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// The employee the signed-in account is linked to, from the <see cref="PlatformClaims.EmployeeId"/> claim.
    /// </summary>
    Guid? EmployeeId { get; }

    /// <summary>
    /// The identifier of the current request, also sent to the client as <c>X-Request-Id</c>.
    /// </summary>
    string? RequestId { get; }
}
