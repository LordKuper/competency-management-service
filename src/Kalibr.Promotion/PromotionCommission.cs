using Kalibr.Platform;

namespace Kalibr.Promotion;

/// <summary>
/// The commission of a unit, kept once the unit's HR or an administrator sets its chair or members. The responsible HR and the head of the unit
/// belong to it without being stored.
/// </summary>
[Audited]
internal sealed class PromotionCommission : IVersioned
{
    [Audited]
    public Guid OrgUnitId { get; init; }

    /// <summary>
    /// The chair, any member of the commission; null while there is none.
    /// </summary>
    [Audited]
    public Guid? ChairEmployeeId { get; set; }

    /// <summary>
    /// The version starts at 1, so that version 0 stands for a commission that is not stored yet.
    /// </summary>
    public int Version { get; private set; } = 1;
}
