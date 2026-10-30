using Kalibr.Platform;

namespace Kalibr.Desk;

/// <summary>
/// One reason of a desk item; the client words it from its kind.
/// </summary>
/// <param name="Kind">What the reason is.</param>
/// <param name="OccurredAt">When the event behind the reason happened or happens, for kinds that come from a dated event.</param>
/// <param name="Details">The values the reason names.</param>
internal sealed record DeskReasonResponse(DeskKind Kind, DateTimeOffset? OccurredAt = null, DeskDetails? Details = null);
