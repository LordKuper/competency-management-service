namespace Competency.OrgStructure;

/// <summary>
/// A unit of the tree with what its card shows besides the unit itself, so that no request per card is needed.
/// The head name and the count of employees directly in the unit are what the caller may see:
/// other users get a working head and working employees only, and a null head name when they may not see the head.
/// </summary>
internal sealed record OrgUnitTreeNodeResponse(
    Guid Id,
    string Name,
    Guid? ParentId,
    Guid? HeadEmployeeId,
    bool IsActive,
    int Version,
    string? HeadName,
    int EmployeeCount);
