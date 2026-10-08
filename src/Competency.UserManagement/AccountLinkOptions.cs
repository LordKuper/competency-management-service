namespace Competency.UserManagement;

/// <summary>
/// How long the links e-mailed to accounts stay usable, bound from the <c>AccountLinks</c> configuration section.
/// </summary>
internal sealed class AccountLinkOptions
{
    public const string Section = "AccountLinks";

    public TimeSpan InvitationLifetime { get; set; }
}
