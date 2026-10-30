using Kalibr.Platform;

namespace Kalibr.Promotion;

/// <summary>
/// The score a candidate gave their own competency in an application.
/// </summary>
[Audited]
internal sealed class PromotionSelfScore
{
    [Audited]
    public Guid ApplicationId { get; init; }

    [Audited]
    public Guid CompetencyId { get; init; }

    /// <summary>
    /// A point of the competency's scale.
    /// </summary>
    [Audited]
    public int Score { get; set; }
}
