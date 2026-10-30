namespace Competency.UserManagement;

/// <summary>
/// How long the links e-mailed to accounts stay usable, and how often an anonymous request may issue a password reset link,
/// bound from the <c>AccountLinks</c> configuration section.
/// </summary>
internal sealed class AccountLinkOptions
{
    public const string Section = "AccountLinks";

    public TimeSpan InvitationLifetime { get; set; }

    public TimeSpan PasswordResetLifetime { get; set; }

    /// <summary>
    /// The least time after a link was issued to an account before an anonymous request may issue it another one; earlier requests are silently skipped,
    /// so nobody can flood a mailbox through the anonymous request.
    /// </summary>
    public TimeSpan PasswordResetInterval { get; set; }
}
