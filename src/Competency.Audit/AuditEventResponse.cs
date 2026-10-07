using System.Text.Json;

namespace Competency.Audit;

/// <summary>
/// One journal row as the list endpoint returns it; the old and new values are the stored JSON objects, not text.
/// </summary>
internal sealed record AuditEventResponse(
    Guid Id,
    DateTimeOffset Timestamp,
    string Actor,
    string Role,
    string Action,
    string EntityType,
    string? EntityId,
    JsonElement? OldValue,
    JsonElement? NewValue,
    string? Reason,
    string? RequestId)
{
    public static AuditEventResponse From(AuditEvent row) => new(
        row.Id,
        row.Timestamp,
        row.Actor,
        row.Role,
        row.Action,
        row.EntityType,
        row.EntityId,
        ParseJson(row.OldValue),
        ParseJson(row.NewValue),
        row.Reason,
        row.RequestId);

    private static JsonElement? ParseJson(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<JsonElement>(json);
}
