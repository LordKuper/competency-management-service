namespace Kalibr.OrgStructure;

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

    /// <summary>
    /// Locks the set of active global administrators until the open transaction ends, so that the answer of
    /// <see cref="IsLastActiveAdministratorAsync"/> stays true while the caller saves; call it before asking.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>A task that completes once the lock is held.</returns>
    Task LockAdministratorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the account bound to the employee is the only active global administrator, so that blocking it would leave none.
    /// </summary>
    /// <param name="employeeId">The employee whose account is checked.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns><see langword="true"/> when the employee's account is the last active administrator; <see langword="false"/> also when there is no account.</returns>
    Task<bool> IsLastActiveAdministratorAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether blocking the accounts bound to all the given employees together would leave no active global administrator, which asking
    /// <see cref="IsLastActiveAdministratorAsync"/> employee by employee cannot tell when several administrators leave at once;
    /// call <see cref="LockAdministratorsAsync"/> first.
    /// </summary>
    /// <param name="employeeIds">The employees who leave together.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns><see langword="true"/> when at least one of them is an active administrator and no active administrator would remain.</returns>
    Task<bool> LeavesNoActiveAdministratorAsync(IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken = default);
}
