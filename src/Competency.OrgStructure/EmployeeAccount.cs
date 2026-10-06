namespace Competency.OrgStructure;

/// <summary>
/// The e-mail of the account bound to an employee, read through a query over the account table that another module owns.
/// It exists so that employee reads show the e-mail in the same query; it is never written or tracked.
/// </summary>
internal sealed class EmployeeAccount
{
    public required Guid EmployeeId { get; init; }

    public required string Email { get; init; }
}
