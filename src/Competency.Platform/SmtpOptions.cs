using MailKit.Security;

namespace Competency.Platform;

/// <summary>
/// The SMTP server mail is sent through, bound from the <c>Smtp</c> configuration section.
/// </summary>
public sealed class SmtpOptions
{
    /// <summary>
    /// Host name or address of the SMTP server.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// TCP port of the SMTP server.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// How the connection is secured; <see cref="SecureSocketOptions.None"/> only for a local mail catcher.
    /// </summary>
    public SecureSocketOptions SecureSocketOptions { get; set; }

    /// <summary>
    /// Whether the server certificate is checked for revocation (CRL/OCSP); turn it off only when the revocation endpoints are unreachable from the host.
    /// The certificate and its chain are validated either way.
    /// </summary>
    public bool CheckCertificateRevocation { get; set; } = true;

    /// <summary>
    /// Sign-in name on the server; empty when the server takes mail without authentication.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Password for <see cref="UserName"/>; a secret, never logged.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Sender mailbox, optionally with a display name (<c>Name &lt;address&gt;</c>).
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Upper bound for one whole send, from connecting to disconnecting, so a caller waiting on it is not held for long.
    /// </summary>
    public TimeSpan Timeout { get; set; }
}
