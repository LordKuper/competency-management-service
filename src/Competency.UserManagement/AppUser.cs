using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Competency.Platform;
using Microsoft.AspNetCore.Identity;

namespace Competency.UserManagement;

/// <summary>
/// A local account that signs in with an e-mail address and password.
/// An account may be bound to one employee, whatever its role, and one employee has at most one account.
/// Accounts are never deleted: blocking ends them.
/// The properties Identity maintains (hash, stamps, lockout) and the e-mailed link are deliberately not audited.
/// </summary>
[Audited]
internal sealed class AppUser : IdentityUser<Guid>, IVersioned
{
    public const int EmailMaxLength = 254;
    public const int PasswordMaxLength = 128;

    /// <summary>
    /// Random bytes in a link token: 256 bits, beyond guessing.
    /// </summary>
    private const int LinkTokenBytes = 32;

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
    /// SHA-256 of the token of the one link e-mailed to the account; the token itself is never stored, so a database leak yields no usable link.
    /// </summary>
    public byte[]? LinkTokenHash { get; private set; }

    public DateTimeOffset? LinkExpiresAt { get; private set; }

    public DateTimeOffset? LinkIssuedAt { get; private set; }

    /// <summary>
    /// The security stamp when the link was issued; anything that ends the account's sessions changes the stamp and so voids the link.
    /// </summary>
    public string? LinkSecurityStamp { get; private set; }

    /// <summary>
    /// An account without a password has been invited and has not yet set one through its invitation link; it cannot sign in.
    /// </summary>
    public bool IsInvited => PasswordHash is null;

    /// <summary>
    /// Sets the e-mail and the Identity user name to the same value: the user name is internal, kept so that Identity's own
    /// lookups and its uniqueness check work, and is never entered or shown. A different address voids the link sent to the previous one.
    /// </summary>
    /// <param name="email">The trimmed, valid e-mail address.</param>
    public void SetEmail(string email)
    {
        if (Email != email)
        {
            ClearLink();
        }

        Email = email;
        UserName = email;
    }

    /// <summary>
    /// Hashes a link token the way it is stored, for looking an account up by the token a link carries.
    /// </summary>
    /// <param name="token">The token from the link.</param>
    /// <returns>The SHA-256 of the token's UTF-8 bytes.</returns>
    public static byte[] HashLinkToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    /// <summary>
    /// Issues a new link, replacing any earlier one, bound to the current security stamp. Call it after the stamp has its final value.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="lifetime">How long the link stays usable.</param>
    /// <returns>The URL-safe token to put in the link; only its hash is kept.</returns>
    public string IssueLink(DateTimeOffset now, TimeSpan lifetime)
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(LinkTokenBytes));
        LinkTokenHash = HashLinkToken(token);
        LinkIssuedAt = now;
        LinkExpiresAt = now + lifetime;
        LinkSecurityStamp = SecurityStamp;
        return token;
    }

    /// <summary>
    /// Whether the link with the given token hash is the account's current one, not expired and issued under the current security stamp.
    /// </summary>
    /// <param name="tokenHash">The hash of the token from the link.</param>
    /// <param name="now">The current time.</param>
    /// <returns><see langword="true"/> when the link may be redeemed.</returns>
    public bool HasValidLink(byte[] tokenHash, DateTimeOffset now) =>
        LinkTokenHash is not null
        && CryptographicOperations.FixedTimeEquals(LinkTokenHash, tokenHash)
        && now < LinkExpiresAt
        && LinkSecurityStamp == SecurityStamp;

    /// <summary>
    /// Voids the account's link, once it is redeemed or its address changes.
    /// </summary>
    public void ClearLink()
    {
        LinkTokenHash = null;
        LinkExpiresAt = null;
        LinkIssuedAt = null;
        LinkSecurityStamp = null;
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
