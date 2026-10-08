using Competency.Platform;
using Microsoft.Extensions.Options;

namespace Competency.UserManagement;

/// <summary>
/// The e-mails sent to accounts, with links built from the configured public address, never from the request.
/// The token travels in the link's fragment, which browsers send to no server, so it stays out of proxy logs and Referer headers.
/// Every send is journaled after it: the event for a sent message, or <c>Mail.SendFailed</c> with the kind of message,
/// never the address, the link or the token. Call it only after the change that issued the link is committed and its locks are released.
/// </summary>
internal sealed class AccountMail(MailSender sender, IOptions<AppOptions> app, IAuditWriter audit)
{
    private const string InvitationRoute = "accept-invitation";
    private const string InvitationSentAction = "AppUser.InvitationSent";
    private const string SendFailedAction = "Mail.SendFailed";
    private const string InvitationKind = "Invitation";
    private const string InitialReason = "Initial";
    private const string RepeatReason = "Repeat";
    private const string InvitationSubject = "Приглашение в систему «Калибр»";

    /// <summary>
    /// Sends the invitation with the account's link and journals the outcome.
    /// </summary>
    /// <param name="user">The invited account.</param>
    /// <param name="token">The token of the link just issued to it.</param>
    /// <param name="isRepeat">Whether the invitation replaces an earlier one.</param>
    /// <param name="cancellationToken">Cancels the send.</param>
    /// <returns><see langword="true"/> when the mail server accepted the message.</returns>
    public async Task<bool> SendInvitationAsync(AppUser user, string token, bool isRepeat, CancellationToken cancellationToken)
    {
        var body = $"""
            Здравствуйте!

            Для вас создана учётная запись в системе «Калибр». Чтобы завершить регистрацию, откройте ссылку и задайте пароль:

            {Link(InvitationRoute, token)}

            Ссылка одноразовая и действует ограниченное время. Если она не открывается, попросите администратора отправить приглашение повторно.

            Если вы не ждали этого письма, просто удалите его.
            """;
        var sent = await sender.SendAsync(user.Email!, InvitationSubject, body, cancellationToken);
        var entry = sent
            ? new AuditEntry(InvitationSentAction, nameof(AppUser), user.Id.ToString(), Reason: isRepeat ? RepeatReason : InitialReason)
            : new AuditEntry(SendFailedAction, nameof(AppUser), user.Id.ToString(), Reason: InvitationKind);
        await audit.WriteAsync(entry, cancellationToken);
        return sent;
    }

    private string Link(string route, string token) => $"{app.Value.PublicBaseUrl.AbsoluteUri.TrimEnd('/')}/{route}#token={token}";
}
