namespace Kalibr.OrgStructure;

/// <summary>
/// How many working employees a unit has, each counted once, by any of their assignments: directly in the unit, and together with all units below it.
/// Employees who do not work are not counted, so the numbers reveal nothing hidden from ordinary users.
/// </summary>
internal sealed record OrgUnitSummaryResponse(Guid UnitId, int EmployeeCount, int DirectEmployeeCount);
