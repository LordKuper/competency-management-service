using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// An employee asked to rate a candidate.
/// </summary>
[Audited]
internal sealed class PromotionRater
{
    [Audited]
    public Guid ApplicationId { get; init; }

    [Audited]
    public Guid EmployeeId { get; init; }

    [Audited]
    public RaterSource Source { get; init; }
}
