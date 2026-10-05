namespace Competency.Platform;

/// <summary>
/// Records events that no tracked entity change describes on its own, such as sign-in and sign-out.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Records one audit event.
    /// </summary>
    /// <param name="entry">The event to record.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
