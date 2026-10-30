using Kalibr.Platform;

namespace Kalibr.Promotion;

/// <summary>
/// An employee, or a position, whose holders may not be asked to rate a candidate or be invited to the system for it. It refers to the employee or
/// position by identifier only, so that removing an employee is never blocked by it.
/// </summary>
[Audited]
internal sealed class PromotionRaterExclusion
{
    [Audited]
    public RaterExclusionKind Kind { get; init; }

    [Audited]
    public Guid SubjectId { get; init; }
}
