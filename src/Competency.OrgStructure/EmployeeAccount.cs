namespace Competency.OrgStructure;

/// <summary>
/// The e-mail and block state of the account bound to an employee, read through a query over the account table that another module owns.
/// It exists so that employee reads show the e-mail in the same query and the impact of leaving shows the account; it is never written or tracked.
/// </summary>
internal sealed class EmployeeAccount
{
    public required Guid EmployeeId { get; init; }

    public required string Email { get; init; }

    public required bool IsBlocked { get; init; }
}
