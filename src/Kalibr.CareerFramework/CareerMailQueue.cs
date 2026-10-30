using System.Threading.Channels;
using Kalibr.OrgStructure;
using Kalibr.Platform;
using Kalibr.UserManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kalibr.CareerFramework;

/// <summary>
/// Sends the career reminders outside the actions that cause them, so an action never waits on the mail server and a failed send never
/// undoes it. Letters wait in memory, one sent at a time: they are lost when the process stops, and a full queue drops new ones, because
/// a reminder only points at what the desk already shows. A letter goes only to an account that can act and is bound to no dismissed employee,
/// as they stand when it is sent.
/// </summary>
internal sealed class CareerMailQueue(
    IServiceScopeFactory scopeFactory,
    MailSender sender,
    IAuditWriter audit,
    ILogger<CareerMailQueue> logger) : BackgroundService
{
    private const int Capacity = 1000;
    private const string SendFailedAction = "Mail.SendFailed";
    private const string RecipientEntityType = "AppUser";

    private readonly Channel<Letter> letters = Channel.CreateBounded<Letter>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true },
        letter => logger.LogWarning("A career reminder of kind {Kind} was dropped: the queue is full.", letter.Kind));

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
                logger.LogError("A career reminder of kind {Kind} failed: {ExceptionType}", letter.Kind, exception.GetType().FullName);
            }
        }
    }

    private async Task SendAsync(Letter letter, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        if (await AddressAsync(letter.RecipientId, scope.ServiceProvider, cancellationToken) is not { } address
            || await sender.SendAsync(address, letter.Subject, letter.Body, cancellationToken))
        {
            return;
        }

        await audit.WriteAsync(
            new AuditEntry(
                SendFailedAction,
                RecipientEntityType,
                letter.RecipientId.ToString(),
                Reason: letter.Kind,
                Actor: letter.ActorId,
                Role: letter.ActorRole,
                RequestId: letter.RequestId),
            cancellationToken);
    }

    /// <summary>
    /// The recipient's address, when the account can act and the employee it is bound to, if any, still works.
    /// </summary>
    private static async Task<string?> AddressAsync(Guid recipientId, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (await services.GetRequiredService<IUserDirectory>().FindAsync(recipientId, cancellationToken) is not { IsActive: true } account)
        {
            return null;
        }

        return account.EmployeeId is { } employeeId
            && await services.GetRequiredService<IEmployeeDirectory>().FindAsync(employeeId, cancellationToken) is not { IsActive: true }
            ? null
            : account.Email;
    }

    /// <summary>
    /// One reminder ready to send, and who caused it, for the journal entry of a failed send.
    /// </summary>
    /// <param name="RecipientId">The recipient's account.</param>
    /// <param name="Kind">The kind of reminder, journaled as the reason of a failed send.</param>
    /// <param name="Subject">The subject line.</param>
    /// <param name="Body">The text body, which <see cref="MailSender"/> also sends as HTML.</param>
    /// <param name="ActorId">The account whose action caused the reminder.</param>
    /// <param name="ActorRole">That account's role.</param>
    /// <param name="RequestId">The request of that action.</param>
    public sealed record Letter(
        Guid RecipientId,
        string Kind,
        string Subject,
        string Body,
        string? ActorId,
        string? ActorRole,
        string? RequestId);
}
