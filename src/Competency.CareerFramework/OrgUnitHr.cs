using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// The HR a global administrator assigned to a unit. The unit and the units below it inherit the assignment down to the first unit
/// with an assignment of its own. Removing the assignment deletes the row.
/// </summary>
[Audited]
internal sealed class OrgUnitHr
{
    [Audited]
    public Guid OrgUnitId { get; init; }

    [Audited]
    public Guid UserId { get; set; }
}
