namespace Competency.CareerFramework;

/// <summary>
/// The employee to assign as a unit's responsible HR; they must work and be bound to an account that can act and has the HR role.
/// </summary>
internal sealed record OrgUnitHrRequest
{
    public required Guid EmployeeId { get; init; }
}
