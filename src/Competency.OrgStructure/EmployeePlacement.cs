namespace Competency.OrgStructure;

/// <summary>
/// One place an employee works at, read through a query that lists the main assignment and every secondary one alike,
/// so that counts and searches over all of an employee's assignments need no union of their own. It is never written or tracked.
/// </summary>
internal sealed class EmployeePlacement
{
    public required Guid EmployeeId { get; init; }

    public required Guid OrgUnitId { get; init; }

    public required Guid PositionId { get; init; }

    /// <summary>
    /// Whether this is the employee's main assignment, the only one that rules about heads, matrices and the like read.
    /// </summary>
    public required bool IsMain { get; init; }
}
