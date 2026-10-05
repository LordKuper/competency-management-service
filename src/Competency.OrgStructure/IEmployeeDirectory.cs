namespace Competency.OrgStructure;

/// <summary>
/// Read access to the employee directory for other modules, which never see the module's entities.
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
}
