using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.OrgStructure;

/// <summary>
/// Reads the assignments and legal entities of employees already read, in a fixed number of queries whatever the number of employees,
/// because a collection inside the employee projection would not keep that number.
/// </summary>
internal static class EmployeeAssignmentReader
{
    /// <summary>
    /// Fills the legal entities of the main assignment, the secondary assignments and, when the list is scoped to units, the assignments in those units.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="employees">The employees as the projection returned them.</param>
    /// <param name="scopeUnitIds">The units whose assignments each employee's <see cref="EmployeeResponse.UnitAssignments"/> lists; <see langword="null"/> for none.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The same employees in the same order, with the lists filled.</returns>
    public static async Task<List<EmployeeResponse>> FillAsync(
        this AppDbContext context,
        IReadOnlyList<EmployeeResponse> employees,
        IReadOnlySet<Guid>? scopeUnitIds,
        CancellationToken cancellationToken)
    {
        if (employees.Count == 0)
        {
            return [];
        }

        var ids = employees.Select(employee => employee.Id).ToArray();
        var mainEntities = (await context.Set<EmployeeLegalEntity>()
            .AsNoTracking()
            .Where(link => ids.Contains(link.EmployeeId))
            .OrderBy(link => link.LegalEntity.Name)
            .ThenBy(link => link.LegalEntityId)
            .Select(link => new { link.EmployeeId, Entity = new LegalEntityResponse(link.LegalEntityId, link.LegalEntity.Name) })
            .ToListAsync(cancellationToken))
            .ToLookup(row => row.EmployeeId, row => row.Entity);
        var secondary = await context.Set<EmployeeAssignment>()
            .AsNoTracking()
            .Where(assignment => ids.Contains(assignment.EmployeeId))
            .OrderBy(assignment => assignment.OrgUnit.Name)
            .ThenBy(assignment => assignment.Position.Name)
            .ThenBy(assignment => assignment.Id)
            .Select(assignment => new { assignment.Id, assignment.EmployeeId, assignment.OrgUnitId, UnitName = assignment.OrgUnit.Name, assignment.PositionId, PositionName = assignment.Position.Name })
            .ToListAsync(cancellationToken);
        var secondaryEntities = (await context.Set<AssignmentLegalEntity>()
            .AsNoTracking()
            .Where(link => ids.Contains(link.Assignment.EmployeeId))
            .OrderBy(link => link.LegalEntity.Name)
            .ThenBy(link => link.LegalEntityId)
            .Select(link => new { link.AssignmentId, Entity = new LegalEntityResponse(link.LegalEntityId, link.LegalEntity.Name) })
            .ToListAsync(cancellationToken))
            .ToLookup(row => row.AssignmentId, row => row.Entity);
        var secondaryOf = secondary.ToLookup(row => row.EmployeeId);

        return [.. employees.Select(employee =>
        {
            var main = mainEntities[employee.Id].ToList();
            var assignments = secondaryOf[employee.Id]
                .Select(row => new AssignmentResponse(row.OrgUnitId, row.UnitName, row.PositionId, row.PositionName, secondaryEntities[row.Id].ToList(), true))
                .ToList();
            var inScope = scopeUnitIds is null
                ? []
                : new[] { new AssignmentResponse(employee.OrgUnitId, employee.OrgUnitName, employee.PositionId, employee.Position, main, false) }
                    .Concat(assignments)
                    .Where(assignment => scopeUnitIds.Contains(assignment.OrgUnitId))
                    .ToList();
            return employee with { LegalEntities = main, Assignments = assignments, UnitAssignments = inScope };
        })];
    }
}
