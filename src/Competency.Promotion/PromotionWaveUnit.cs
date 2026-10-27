using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// A unit that takes part in a wave.
/// </summary>
[Audited]
internal sealed class PromotionWaveUnit
{
    [Audited]
    public Guid WaveId { get; init; }

    [Audited]
    public Guid OrgUnitId { get; init; }
}
