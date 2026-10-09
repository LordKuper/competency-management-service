using System.Linq.Expressions;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.OrgStructure;

/// <summary>
/// Answers <see cref="IEmployeeDirectory"/> lookups straight from the employee and unit tables.
/// </summary>
internal sealed class EmployeeDirectory(AppDbContext context) : IEmployeeDirectory
{
    private static readonly Expression<Func<Employee, EmployeeStatus>> StatusProjection = employee => new EmployeeStatus(
        employee.Id, employee.FullName, employee.LastName, employee.FirstName, employee.MiddleName, employee.IsActive);

    private static readonly Expression<Func<OrgUnit, OrgUnitStatus>> UnitStatusProjection = unit => new OrgUnitStatus(
        unit.Id, unit.Name, unit.ParentId, unit.HeadEmployeeId, unit.IsActive);

    public Task<EmployeeStatus?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default) => context
        .Set<Employee>()
        .AsNoTracking()
        .Where(employee => employee.Id == employeeId)
        .Select(StatusProjection)
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, EmployeeStatus>> FindManyAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToArray();
        return await context
            .Set<Employee>()
            .AsNoTracking()
            .Where(employee => ids.Contains(employee.Id))
            .Select(StatusProjection)
            .ToDictionaryAsync(status => status.Id, cancellationToken);
    }

    public Task<OrgUnitStatus?> FindUnitAsync(Guid unitId, CancellationToken cancellationToken = default) => context
        .Set<OrgUnit>()
        .AsNoTracking()
        .Where(unit => unit.Id == unitId)
        .Select(UnitStatusProjection)
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OrgUnitStatus>> FindAncestryAsync(Guid unitId, CancellationToken cancellationToken = default) =>
        await context.Ancestry(unitId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrgUnitStatus>> FindHeadedUnitsAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await context
            .Set<OrgUnit>()
            .AsNoTracking()
            .Where(unit => unit.HeadEmployeeId == employeeId)
            .OrderBy(unit => unit.Name)
            .ThenBy(unit => unit.Id)
            .Select(UnitStatusProjection)
            .ToListAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginExclusiveAsync(CancellationToken cancellationToken = default) =>
        context.BeginExclusiveAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginSharedAsync(CancellationToken cancellationToken = default) =>
        context.BeginSharedAsync(cancellationToken);
}
