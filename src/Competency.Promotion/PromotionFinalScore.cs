using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// The score the chair of the commission gives a competency of a candidate; the rating and the readiness are worked out from these alone.
/// </summary>
[Audited]
internal sealed class PromotionFinalScore
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
