namespace Competency.OrgStructure;

/// <summary>
/// An assignment of an employee as the API returns it: the unit and the position with their current names, the legal entities by name,
/// and whether it is a secondary assignment, which a card marks as a combination of positions.
/// </summary>
internal sealed record AssignmentResponse(
    Guid OrgUnitId,
    string OrgUnitName,
    Guid PositionId,
    string Position,
    IReadOnlyList<LegalEntityResponse> LegalEntities,
    bool IsSecondary);
