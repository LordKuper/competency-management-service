using Competency.Platform;

namespace Competency.Audit;

/// <summary>
/// Turns an <see cref="AuditEntry"/> into a journal row stamped with the current time and the ambient actor,
/// falling back to the <c>system</c> actor and role when no user is signed in, such as at startup or in background work.
/// </summary>
internal sealed class AuditEventFactory(ICurrentActor currentActor, TimeProvider timeProvider)
{
    private const string SystemName = "system";

    public AuditEvent Create(AuditEntry entry) => new()
    {
        Timestamp = timeProvider.GetUtcNow(),
        Actor = entry.Actor ?? currentActor.UserId?.ToString() ?? SystemName,
        Role = entry.Role ?? currentActor.Role ?? SystemName,
        Action = entry.Action,
        EntityType = entry.EntityType,
        EntityId = entry.EntityId,
        OldValue = entry.OldValue,
        NewValue = entry.NewValue,
        Reason = entry.Reason,
        RequestId = entry.RequestId ?? currentActor.RequestId,
    };
}
