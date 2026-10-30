using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Competency.Audit;

/// <summary>
/// Saves each explicit event through its own context, connection and transaction, so it neither flushes
/// the caller's pending changes nor rolls back with them, or stages it on the caller's context, so it is saved
/// and rolled back with the caller's changes. Never logs the event.
/// </summary>
internal sealed class AuditWriter(IServiceScopeFactory scopeFactory, AuditEventFactory factory) : IAuditWriter
{
    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Set<AuditEvent>().Add(factory.Create(entry));
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Stage(AuditEntry entry, DbContext context) => context.Set<AuditEvent>().Add(factory.Create(entry));
}
