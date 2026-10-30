using Kalibr.Platform;

namespace Kalibr.CareerFramework;

/// <summary>
/// The employee a global administrator assigned to a unit as its responsible HR; the assignment acts through the account bound to the employee at the moment. The unit and the units below it inherit the assignment down to the first unit
/// with an assignment of its own. Removing the assignment deletes the row.
/// </summary>
[Audited]
internal sealed class OrgUnitHr
{
    [Audited]
    public Guid OrgUnitId { get; init; }

    [Audited]
    public Guid EmployeeId { get; set; }
}
