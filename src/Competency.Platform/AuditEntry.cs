namespace Competency.Platform;

/// <summary>
/// One audit event as its raiser describes it; the writer adds the time, and the actor, role and request id unless overridden.
/// </summary>
/// <param name="Action">What happened, such as a created entity or a failed sign-in.</param>
/// <param name="EntityType">The kind of entity the event concerns.</param>
/// <param name="EntityId">The identifier of that entity, if the event concerns one.</param>
/// <param name="Reason">A short reason for the action, if one was given.</param>
/// <param name="OldValue">The audited property values before the action, as JSON.</param>
/// <param name="NewValue">The audited property values after the action, as JSON.</param>
/// <param name="Actor">Overrides the ambient actor for events raised before a principal exists, such as sign-in.</param>
/// <param name="Role">Overrides the ambient role together with <paramref name="Actor"/>.</param>
public sealed record AuditEntry(
    string Action,
    string EntityType,
    string? EntityId = null,
    string? Reason = null,
    string? OldValue = null,
    string? NewValue = null,
    string? Actor = null,
    string? Role = null);
