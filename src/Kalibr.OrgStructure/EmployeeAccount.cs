namespace Kalibr.OrgStructure;

/// <summary>
/// The e-mail, block state and HR candidacy of the account bound to an employee, read through a query over the account table that another module owns.
/// It exists so that employee reads show the e-mail in the same query and the impact of leaving shows the account; it is never written or tracked.
/// </summary>
internal sealed class EmployeeAccount
{
    public required Guid EmployeeId { get; init; }

    public required string Email { get; init; }

    public required bool IsBlocked { get; init; }

    /// <summary>
    /// Whether the account makes the employee a candidate for responsible HR: it is neither blocked nor invited and has the HR role.
    /// The account module's own rule, restated in SQL because this module cannot reference it; the binding comes from the query itself.
    /// </summary>
    public required bool CanBeHr { get; init; }
}
