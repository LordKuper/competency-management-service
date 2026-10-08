using System.Threading.Channels;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Competency.UserManagement;

/// <summary>
/// Issues and e-mails password reset links for anonymous requests after the request has been answered, so neither the response nor its timing
/// tells whether an account has the address. Requests wait in memory, one handled at a time: they are lost when the process stops, and the user asks again;
/// a full queue drops new ones. Nothing is stored for an address no account has. A link goes only to an account that may reset its password,
/// and not again until <see cref="AccountLinkOptions.PasswordResetInterval"/> has passed since the account's last link.
/// </summary>
internal sealed class PasswordResetQueue(
    IServiceScopeFactory scopeFactory,
    AccountMail mail,
    IAuditWriter audit,
    TimeProvider time,
    IOptions<AccountLinkOptions> links,
    ILogger<PasswordResetQueue> logger) : BackgroundService
{
    /// <summary>
    /// The journal action of an issued password reset link, whoever asked for it.
    /// </summary>
    public const string RequestedAction = "Auth.PasswordResetRequested";

    private const int Capacity = 1000;

    private readonly Channel<Request> requests = Channel.CreateBounded<Request>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true },
        _ => logger.LogWarning("A password reset request was dropped: the queue is full."));

    /// <summary>
    /// Queues a request without waiting; it never fails and never tells whether an account has the address.
    /// </summary>
    /// <param name="email">The trimmed e-mail address from the request.</param>
    /// <param name="requestId">The id of the request, journaled with the link it issues.</param>
    public void Enqueue(string email, string? requestId) => requests.Writer.TryWrite(new Request(email, requestId));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in requests.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await HandleAsync(request, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError("A password reset request failed: {ExceptionType}", exception.GetType().FullName);
            }
        }
    }

    private async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(request.Email);
        if (user is null)
        {
            return;
        }

        if (await IssueAsync(user, scope.ServiceProvider, request.RequestId, cancellationToken) is { } token)
        {
            await mail.SendPasswordResetAsync(user, token, request.RequestId, cancellationToken);
        }
    }

    /// <summary>
    /// Issues the link under the account row lock, against the account as committed, and saves it with its journal event.
    /// </summary>
    /// <returns>The token to e-mail, or <see langword="null"/> when the account may not reset its password or got a link too recently.</returns>
    private async Task<string?> IssueAsync(AppUser user, IServiceProvider services, string? requestId, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var now = time.GetUtcNow();
        await using var transaction = await context.BeginAccountLockAsync(user.Id, cancellationToken);
        await context.Entry(user).ReloadAsync(cancellationToken);
        if (!await services.GetRequiredService<IEmployeeDirectory>().MayResetPasswordAsync(user, cancellationToken)
            || now - user.LinkIssuedAt < links.Value.PasswordResetInterval)
        {
            return null;
        }

        var token = user.IssueLink(now, links.Value.PasswordResetLifetime);
        audit.Stage(
            new AuditEntry(RequestedAction, nameof(AppUser), user.Id.ToString(), Actor: user.Id.ToString(), Role: user.Role.ToString(), RequestId: requestId),
            context);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return token;
    }

    private sealed record Request(string Email, string? RequestId);
}
