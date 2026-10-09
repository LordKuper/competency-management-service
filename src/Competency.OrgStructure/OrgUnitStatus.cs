namespace Competency.OrgStructure;

/// <summary>
/// The facts about an organization unit that other modules may rely on.
/// </summary>
/// <param name="Id">The unit identifier.</param>
/// <param name="Name">The unit name.</param>
/// <param name="ParentId">The parent unit; null for a root.</param>
/// <param name="HeadEmployeeId">The employee who heads the unit; null when it has no head.</param>
/// <param name="IsActive">Whether the unit is in use rather than deactivated.</param>
public sealed record OrgUnitStatus(Guid Id, string Name, Guid? ParentId, Guid? HeadEmployeeId, bool IsActive);
