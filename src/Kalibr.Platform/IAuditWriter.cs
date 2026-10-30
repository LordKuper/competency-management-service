using Microsoft.EntityFrameworkCore;

namespace Competency.Platform;

/// <summary>
/// Records events that no tracked entity change describes on its own, such as sign-in and sign-out.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Records one audit event, saved at once through a context and transaction of its own, so that it stays
    /// even when the caller's own changes are rolled back; for events about refused or failed actions.
    /// </summary>
    /// <param name="entry">The event to record.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds one audit event to the caller's context without saving it, so that the caller's next save writes it in the same
    /// transaction as the caller's other changes and the event exists exactly when those changes do; for events about a change that succeeded.
    /// Add it right before the save that stores the change, because any save of the context stores it.
    /// </summary>
    /// <param name="entry">The event to record.</param>
    /// <param name="context">The context whose next save stores the event.</param>
    void Stage(AuditEntry entry, DbContext context);
}
