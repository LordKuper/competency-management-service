using Kalibr.Platform;

namespace Kalibr.Promotion;

/// <summary>
/// The published version of the track matrix in effect in a unit of a wave, its own or inherited, that the wave is bound to for that unit when
/// its first stage starts; the wave assesses by this version whatever is published later. A matrix inherited by several units of the wave is
/// bound once for each.
/// </summary>
[Audited]
internal sealed class PromotionWaveMatrix
{
    [Audited]
    public Guid WaveId { get; init; }

    [Audited]
    public Guid MatrixId { get; init; }

    [Audited]
    public Guid OrgUnitId { get; init; }

    [Audited]
    public Guid VersionId { get; init; }

    [Audited]
    public Guid TrackId { get; init; }

    /// <summary>
    /// The track name as the version froze it.
    /// </summary>
    [Audited]
    public string TrackName { get; init; } = string.Empty;
}
