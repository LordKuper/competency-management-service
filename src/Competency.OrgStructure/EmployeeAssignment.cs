using Competency.Platform;

namespace Competency.OrgStructure;

/// <summary>
/// A secondary assignment of an employee: a unit and a position besides the main assignment, which stays in the employee's own unit and position fields.
/// The pair of unit and position is never repeated among one employee's assignments, the main one included; the code checks it under the tree lock,
/// and the database keeps the secondary pairs unique. Deleting the employee deletes the assignment.
/// </summary>
[Audited]
internal sealed class EmployeeAssignment : EntityBase
{
    [Audited]
    public Guid EmployeeId { get; init; }

    [Audited]
    public Guid OrgUnitId { get; init; }

    [Audited]
    public Guid PositionId { get; init; }

    public OrgUnit OrgUnit { get; init; } = null!;

    public Position Position { get; init; } = null!;

    /// <summary>
    /// The links to the legal entities of this assignment, loaded only when a change needs them.
    /// </summary>
    public List<AssignmentLegalEntity> LegalEntities { get; } = [];
}
