namespace Competency.OrgStructure;

/// <summary>
/// The move of one assignment of an employee to another unit, the assignment being named by its unit and position, which are unique among the employee's assignments.
/// Every field is required so that omitting one never moves the wrong assignment.
/// </summary>
internal sealed record MoveEmployeeRequest
{
    public required Guid FromOrgUnitId { get; init; }

    public required Guid PositionId { get; init; }

    public required Guid ToOrgUnitId { get; init; }
}
