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

    /// <summary>
    /// Whether the letter about applications held for want of a chair has been sent for this unit; it is sent once per wave.
    /// </summary>
    public bool NoChairLetterSent { get; set; }
}
