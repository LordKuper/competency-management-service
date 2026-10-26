using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.OrgStructure;

/// <summary>
/// Access to employees and organization units for other modules, which never see the module's entities.
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
    /// Finds an organization unit.
    /// </summary>
    /// <param name="unitId">The unit identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The unit's facts, or <see langword="null"/> when no such unit exists.</returns>
    Task<OrgUnitStatus?> FindUnitAsync(Guid unitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds many organization units in one query.
    /// </summary>
    /// <param name="unitIds">The unit identifiers.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The facts of each unit that exists, by identifier; identifiers of units that do not exist are absent.</returns>
    Task<IReadOnlyDictionary<Guid, OrgUnitStatus>> FindUnitsAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a unit and every unit above it, for rules that apply to the nearest unit up the tree that meets a condition, such as having a head.
    /// </summary>
    /// <param name="unitId">The unit the chain starts from.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The unit first, then its parent and so on up to the root; empty when the unit does not exist.</returns>
    Task<IReadOnlyList<OrgUnitStatus>> FindAncestryAsync(Guid unitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a unit and every unit below it, for rules a unit passes down to the units under it, such as an assignment they inherit.
    /// </summary>
    /// <param name="unitId">The top of the subtree.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The units of the subtree, active or not, in no particular order; empty when the unit does not exist.</returns>
    Task<IReadOnlyList<OrgUnitStatus>> FindSubtreeAsync(Guid unitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the units an employee heads. Nothing in the data limits them to the employee's own unit, so there may be several, in any part of the tree.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The headed units, active or not, in name order; empty when the employee heads none or does not exist.</returns>
    Task<IReadOnlyList<OrgUnitStatus>> FindHeadedUnitsAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the whole organization structure in a fixed number of queries, for callers that check many units at once and would
    /// otherwise query per unit. The snapshot is remembered for the rest of the request; taking the tree lock through this directory
    /// forgets it, so a caller holding the lock reads one the lock protects. The caller's own changes do not refresh it.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The snapshot, with the module's tree and position rules.</returns>
    Task<OrgSnapshot> FindOrgSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a transaction and takes the lock every employee and unit change holds, until the transaction ends, so that what the caller
    /// then reads about employees, such as whether one works, cannot change before the caller commits. A caller that also locks the
    /// active administrators must take this lock first, as dismissing an employee does, or the two could wait for each other.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the lock.</returns>
    Task<IDbContextTransaction> BeginExclusiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a transaction and takes the same lock in shared mode, until the transaction ends, so that rights the caller derives from
    /// units, their heads and employees cannot change before it commits, while other such readers run alongside. It must be the caller's
    /// first lock, before any of its own, and a caller that may change units or employees takes <see cref="BeginExclusiveAsync"/> instead.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the lock.</returns>
    Task<IDbContextTransaction> BeginSharedAsync(CancellationToken cancellationToken = default);
}
