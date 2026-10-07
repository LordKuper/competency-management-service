using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.OrgStructure;

/// <summary>
/// Access to the employee directory for other modules, which never see the module's entities.
/// Implementations are scoped to a request, because they share its database context.
/// </summary>
public interface IEmployeeDirectory
{
    /// <summary>
    /// Finds an employee and whether they work in the organization.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The employee's status, or <see langword="null"/> when no such employee exists.</returns>
    Task<EmployeeStatus?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds many employees in one query.
    /// </summary>
    /// <param name="employeeIds">The employee identifiers.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The status of each employee that exists, by identifier; identifiers of employees that do not exist are absent.</returns>
    Task<IReadOnlyDictionary<Guid, EmployeeStatus>> FindManyAsync(IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a transaction and takes the lock every employee and unit change holds, until the transaction ends, so that what the caller
    /// then reads about employees, such as whether one works, cannot change before the caller commits. A caller that also locks the
    /// active administrators must take this lock first, as dismissing an employee does, or the two could wait for each other.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the lock.</returns>
    Task<IDbContextTransaction> BeginExclusiveAsync(CancellationToken cancellationToken = default);
}
