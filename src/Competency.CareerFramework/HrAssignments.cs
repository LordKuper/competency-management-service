using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// Answers <see cref="IHrAssignments"/> from the unit HR assignments, on the request's database context, so a removal is saved and journaled
/// together with the caller's own change.
/// </summary>
internal sealed class HrAssignments(AppDbContext context, IEmployeeDirectory employees) : IHrAssignments
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> UnitNamesAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var unitIds = await context.Set<OrgUnitHr>()
            .Where(assignment => assignment.EmployeeId == employeeId)
            .Select(assignment => assignment.OrgUnitId)
            .ToListAsync(cancellationToken);
        if (unitIds.Count == 0)
        {
            return [];
        }

        var units = await employees.FindUnitsAsync(unitIds, cancellationToken);
        return [.. units.Values.Select(unit => unit.Name).Order(StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid employeeId, CancellationToken cancellationToken) =>
        context.RemoveRange(await context.Set<OrgUnitHr>().Where(assignment => assignment.EmployeeId == employeeId).ToListAsync(cancellationToken));
}
