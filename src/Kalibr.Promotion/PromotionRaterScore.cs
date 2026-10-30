using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// The answer of a rater about one competency of a candidate. The journal keeps who gave it; nothing the API serves does.
/// </summary>
[Audited]
internal sealed class PromotionRaterScore
{
    [Audited]
    public Guid ApplicationId { get; init; }

    /// <summary>
    /// The rater.
    /// </summary>
    [Audited]
    public Guid EmployeeId { get; init; }

    [Audited]
    public Guid CompetencyId { get; init; }

    /// <summary>
    /// A point of the competency's scale; null when the rater did not observe the competency.
    /// </summary>
    [Audited]
    public int? Score { get; set; }
}
