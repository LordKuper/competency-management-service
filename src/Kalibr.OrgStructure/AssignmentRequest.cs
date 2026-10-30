namespace Competency.OrgStructure;

/// <summary>
/// A secondary assignment of an employee as a request names it: the unit and position and the legal entities of the assignment.
/// </summary>
internal sealed record AssignmentRequest
{
    public required Guid OrgUnitId { get; init; }

    public required Guid PositionId { get; init; }

    public required IReadOnlyList<Guid> LegalEntityIds { get; init; }
}
