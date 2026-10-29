using System.Net;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Competency.Platform;

/// <summary>
/// Sends mail through the SMTP server from <see cref="SmtpOptions"/>, one connection per message, as <c>multipart/alternative</c>:
/// the text body as given, and an HTML body built from it by <see cref="ToHtml"/>, so composers write text only.
/// The HTML follows one rule. Blocks separated by blank lines become paragraphs, single line breaks inside a block become <c>&lt;br&gt;</c>,
/// and all text is HTML-encoded, so a name or a comment never becomes markup. A line that is a single absolute http(s) address becomes a link
/// whose text is the line directly above it with the trailing colon removed (that line is not shown twice), or the address itself when no line
/// stands above it in its block. A composer therefore puts a short action line such as «Подать заявку:» directly above each address.
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
    /// <param name="body">The text body, written by the rule above.</param>
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
        var message = new MimeMessage { Subject = subject, Body = new BodyBuilder { TextBody = body, HtmlBody = ToHtml(body) }.ToMessageBody() };
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(recipient));
        return message;
    }

    /// <summary>
    /// The HTML document of a text body by the rule in the type description.
    /// </summary>
    /// <param name="text">The text body.</param>
    internal static string ToHtml(string text)
    {
        var paragraphs = new List<string>();
        var block = new List<string>();
        foreach (var line in text.ReplaceLineEndings("\n").Split('\n').Select(line => line.Trim()).Append(string.Empty))
        {
            if (line.Length > 0)
            {
                block.Add(line);
            }
            else if (block.Count > 0)
            {
                paragraphs.Add($"<p>{string.Join("<br>", Fragments(block))}</p>");
                block.Clear();
            }
        }

        return $"<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\"></head><body>{string.Join(string.Empty, paragraphs)}</body></html>";
    }

    private static IEnumerable<string> Fragments(List<string> lines)
    {
        string? pending = null;
        foreach (var line in lines)
        {
            if (!IsWebAddress(line))
            {
                if (pending is not null)
                {
                    yield return WebUtility.HtmlEncode(pending);
                }

                pending = line;
                continue;
            }

            var label = pending?.TrimEnd(':').TrimEnd();
            pending = null;
            yield return $"<a href=\"{WebUtility.HtmlEncode(line)}\">{WebUtility.HtmlEncode(string.IsNullOrEmpty(label) ? line : label)}</a>";
        }

        if (pending is not null)
        {
            yield return WebUtility.HtmlEncode(pending);
        }
    }

    private static bool IsWebAddress(string line) =>
        !line.Any(char.IsWhiteSpace)
        && Uri.TryCreate(line, UriKind.Absolute, out var address)
        && (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps);
}
