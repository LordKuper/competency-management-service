namespace Competency.Audit;

/// <summary>
/// One row of the append-only audit journal: who did what to which entity, when, and under which request.
/// </summary>
internal sealed class AuditEvent
{
    public Guid Id { get; init; }

    /// <summary>
    /// The UTC time the event was recorded.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The identifier of the user account as text, or <c>system</c> for work no signed-in user triggered.
    /// </summary>
    public required string Actor { get; init; }

    /// <summary>
    /// The role the actor held, or <c>system</c> together with the system actor.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// What happened, such as <c>OrgUnit.Updated</c>.
    /// </summary>
    public required string Action { get; init; }

    public required string EntityType { get; init; }

    /// <summary>
    /// The identifier of the entity concerned; absent for events that concern none, such as a failed sign-in.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// A JSON object with the audited property values before the action; only the changed properties for an update.
    /// </summary>
    public string? OldValue { get; init; }

    /// <summary>
    /// A JSON object with the audited property values after the action; only the changed properties for an update.
    /// </summary>
    public string? NewValue { get; init; }

    public string? Reason { get; init; }

    public string? RequestId { get; init; }
}
