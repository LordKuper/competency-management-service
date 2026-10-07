namespace Competency.OrgStructure;

/// <summary>
/// What happens to the account bound to an employee when the employee stops working or is deleted.
/// The module that owns accounts implements it, so the employee module never reaches into them.
/// Implementations are scoped to a request, because they share its database context.
/// </summary>
public interface IEmployeeAccounts
{
    /// <summary>
    /// Blocks the account bound to the employee, ending its sessions, and optionally detaches it from the employee.
    /// The changes are only tracked on the shared database context: the caller saves them in its own transaction.
    /// Does nothing when the employee has no account, and leaves an already blocked account blocked.
    /// </summary>
    /// <param name="employeeId">The employee whose account is blocked.</param>
    /// <param name="unbind">Whether the account is also detached from the employee.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>A task that completes when the changes are tracked.</returns>
    Task BlockAsync(Guid employeeId, bool unbind, CancellationToken cancellationToken = default);
}
