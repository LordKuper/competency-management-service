namespace Competency.OrgStructure;

/// <summary>
/// The number of working employees who hold a position and keep it when it is deactivated or taken off a unit's list.
/// </summary>
internal sealed record PositionImpactResponse(int EmployeeCount);
