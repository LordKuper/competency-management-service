using System.Threading.Channels;
using Kalibr.OrgStructure;
using Kalibr.Platform;
using Kalibr.UserManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kalibr.Promotion;

/// <summary>
/// Sends the promotion letters outside the actions that cause them, so a stage change or a request never waits on the mail server and a failed
/// send never undoes it. Letters wait in memory, one sent at a time: they are lost when the process stops, and a full queue drops new ones, because
/// a letter only points at what the desk already shows. A letter goes only to an account that can act, or for a letter marked so one that was invited and has not set its password yet, and is bound to no
/// dismissed employee, as they stand when it is sent. A letter with the same text for many accounts is one entry of the queue.
/// </summary>
internal sealed class PromotionMailQueue(
    IServiceScopeFactory scopeFactory,
    MailSender sender,
    IAuditWriter audit,
    ILogger<PromotionMailQueue> logger) : BackgroundService
{
    private const int Capacity = 1000;
    private const string SendFailedAction = "Mail.SendFailed";
    private const string RecipientEntityType = "AppUser";

    private readonly Channel<Letter> letters = Channel.CreateBounded<Letter>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true },
        letter => logger.LogWarning("A promotion letter of kind {Kind} was dropped: the queue is full.", letter.Kind));

    /// <summary>
    /// Queues a ready letter without waiting; it never fails.
    /// </summary>
    /// <param name="letter">The letter.</param>
    public void Enqueue(Letter letter) => letters.Writer.TryWrite(letter);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var letter in letters.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(letter, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError("A promotion letter of kind {Kind} failed: {ExceptionType}", letter.Kind, exception.GetType().FullName);
            }
        }
    }

    private async Task SendAsync(Letter letter, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        foreach (var recipientId in letter.RecipientIds)
        {
            if (await AddressAsync(recipientId, letter.ToInvited, scope.ServiceProvider, cancellationToken) is not { } address
                || await sender.SendAsync(address, letter.Subject, letter.Body, cancellationToken))
            {
                continue;
            }

            await audit.WriteAsync(
                new AuditEntry(
                    SendFailedAction,
                    RecipientEntityType,
                    recipientId.ToString(),
                    Reason: letter.Kind,
                    Actor: letter.ActorId,
                    Role: letter.ActorRole,
                    RequestId: letter.RequestId),
                cancellationToken);
        }
    }

    /// <summary>
    /// The recipient's address, when the account can act (or may be invited) and the employee it is bound to, if any, still works.
    /// </summary>
    private static async Task<string?> AddressAsync(Guid recipientId, bool toInvited, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (await services.GetRequiredService<IUserDirectory>().FindAsync(recipientId, cancellationToken) is not { } account
            || account.IsBlocked
            || (!toInvited && !account.IsRegistered))
        {
            return null;
        }

        return account.EmployeeId is { } employeeId
            && await services.GetRequiredService<IEmployeeDirectory>().FindAsync(employeeId, cancellationToken) is not { IsActive: true }
            ? null
            : account.Email;
    }

    /// <summary>
    /// One letter ready to send to its recipients, and who caused it, for the journal entry of a failed send.
    /// </summary>
    /// <param name="RecipientIds">The recipients' accounts.</param>
    /// <param name="Kind">The kind of letter, journaled as the reason of a failed send.</param>
    /// <param name="Subject">The subject line.</param>
    /// <param name="Body">The text body, which <see cref="MailSender"/> also sends as HTML.</param>
    /// <param name="ActorId">The account whose action caused the letter; null for a letter of a stage change.</param>
    /// <param name="ActorRole">That account's role.</param>
    /// <param name="RequestId">The request of that action.</param>
    /// <param name="ToInvited">Whether the letter also goes to an account that was invited and has not set its password yet.</param>
    public sealed record Letter(
        IReadOnlyCollection<Guid> RecipientIds,
        string Kind,
        string Subject,
        string Body,
        string? ActorId,
        string? ActorRole,
        string? RequestId,
        bool ToInvited = false);
}
