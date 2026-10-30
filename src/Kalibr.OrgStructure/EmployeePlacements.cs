using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.OrgStructure;

/// <summary>
/// Queries over the places working employees work at, by any of their assignments, for the counts a unit or a position shows.
/// </summary>
internal static class EmployeePlacements
{
    /// <summary>
    /// The placements of working employees: the main and the secondary assignments alike.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <returns>A query to compose into larger queries.</returns>
    public static IQueryable<EmployeePlacement> WorkingPlacements(this AppDbContext context)
    {
        var employees = context.Set<Employee>();
        return context.Set<EmployeePlacement>()
            .Where(placement => employees.Any(employee => employee.Id == placement.EmployeeId && employee.IsActive));
    }

    /// <summary>
    /// Counts the employees the placements belong to, each once however many of the placements are theirs.
    /// </summary>
    /// <param name="placements">The placements.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The number of distinct employees.</returns>
    public static Task<int> CountEmployeesAsync(this IQueryable<EmployeePlacement> placements, CancellationToken cancellationToken) =>
        placements.Select(placement => placement.EmployeeId).Distinct().CountAsync(cancellationToken);
}
