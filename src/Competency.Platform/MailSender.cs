using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Competency.Platform;

/// <summary>
/// Sends plain-text mail through the SMTP server from <see cref="SmtpOptions"/>, one connection per message.
/// Thread-safe: it holds no connection between calls, so request handlers and background services share one instance.
/// Never call it inside a database transaction or under a lock: it waits on the network for up to <see cref="SmtpOptions.Timeout"/>.
/// </summary>
public sealed class MailSender(IOptions<SmtpOptions> options, ILogger<MailSender> logger)
{
    /// <summary>
    /// Sends one message. A failure is logged by exception type only, so the recipient, the content and the server reply stay out of the log.
    /// </summary>
    /// <param name="recipient">The recipient's e-mail address.</param>
    /// <param name="subject">The subject line.</param>
    /// <param name="body">The plain-text body.</param>
    /// <param name="cancellationToken">Cancels the send; a cancellation by the caller is rethrown, not reported as a failure.</param>
    /// <returns><see langword="true"/> when the server accepted the message; <see langword="false"/> when sending failed or timed out.</returns>
    public async Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.Timeout);
        try
        {
            using var client = new SmtpClient { CheckCertificateRevocation = settings.CheckCertificateRevocation };
            await client.ConnectAsync(settings.Host, settings.Port, settings.SecureSocketOptions, timeout.Token);
            if (!string.IsNullOrEmpty(settings.UserName))
            {
                await client.AuthenticateAsync(settings.UserName, settings.Password, timeout.Token);
            }

            await client.SendAsync(CreateMessage(settings.From, recipient, subject, body), timeout.Token);
            await client.DisconnectAsync(true, timeout.Token);
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Mail was not sent: {ExceptionType}", exception.GetType().FullName);
            return false;
        }
    }

    private static MimeMessage CreateMessage(string from, string recipient, string subject, string body)
    {
        var message = new MimeMessage { Subject = subject, Body = new TextPart(TextFormat.Plain) { Text = body } };
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(recipient));
        return message;
    }
}
