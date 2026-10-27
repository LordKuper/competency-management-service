using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// The published version of a unit's track matrix a wave is bound to when its first stage starts; the wave assesses by this version
/// whatever is published later.
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
