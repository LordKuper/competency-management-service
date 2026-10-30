using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// An employee a commission has been given besides the members it always has.
/// </summary>
[Audited]
internal sealed class PromotionCommissionMember
{
    [Audited]
    public Guid OrgUnitId { get; init; }

    [Audited]
    public Guid EmployeeId { get; init; }
}
