namespace Competency.Platform;

/// <summary>
/// The units an employee is assigned to as their responsible HR, for the modules that cannot reference the one that keeps the assignments:
/// a leaving employee loses them, and an account keeps the HR role while its employee holds any.
/// Implementations are scoped to a request, because they share its database context.
/// </summary>
public interface IHrAssignments
{
    /// <summary>
    /// Names the units of any status the employee is assigned to as responsible HR.
    /// </summary>
    /// <param name="employeeId">The employee.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The unit names in alphabetical order; empty when the employee has no assignment.</returns>
    Task<IReadOnlyList<string>> UnitNamesAsync(Guid employeeId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes every assignment of the employee. The removal is only tracked on the shared database context: the caller saves it in its own
    /// transaction, under the tree lock, and each removed assignment is journaled.
    /// </summary>
    /// <param name="employeeId">The employee.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>A task that completes when the removal is tracked.</returns>
    Task RemoveAsync(Guid employeeId, CancellationToken cancellationToken);
}
