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
        employee.Id, employee.FullName, employee.LastName, employee.FirstName, employee.MiddleName, employee.IsActive,
        employee.OrgUnitId, employee.PositionId);

    private static readonly Expression<Func<OrgUnit, OrgUnitStatus>> UnitStatusProjection = unit => new OrgUnitStatus(
        unit.Id, unit.Name, unit.ParentId, unit.HeadEmployeeId, unit.IsActive);

    private OrgSnapshot? snapshot;

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

    public async Task<IReadOnlyDictionary<Guid, OrgUnitStatus>> FindUnitsAsync(
        IReadOnlyCollection<Guid> unitIds,
        CancellationToken cancellationToken = default)
    {
        var ids = unitIds.ToArray();
        return await context
            .Set<OrgUnit>()
            .AsNoTracking()
            .Where(unit => ids.Contains(unit.Id))
            .Select(UnitStatusProjection)
            .ToDictionaryAsync(status => status.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrgUnitStatus>> FindAncestryAsync(Guid unitId, CancellationToken cancellationToken = default) =>
        await context.Ancestry(unitId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrgUnitStatus>> FindSubtreeAsync(Guid unitId, CancellationToken cancellationToken = default)
    {
        var subtreeIds = context.DescendantIds(unitId);
        return await context
            .Set<OrgUnit>()
            .AsNoTracking()
            .Where(unit => subtreeIds.Contains(unit.Id))
            .Select(UnitStatusProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrgUnitStatus>> FindHeadedUnitsAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await context
            .Set<OrgUnit>()
            .AsNoTracking()
            .Where(unit => unit.HeadEmployeeId == employeeId)
            .OrderBy(unit => unit.Name)
            .ThenBy(unit => unit.Id)
            .Select(UnitStatusProjection)
            .ToListAsync(cancellationToken);

    public async Task<OrgSnapshot> FindOrgSnapshotAsync(CancellationToken cancellationToken = default) =>
        snapshot ??= await ReadOrgSnapshotAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginExclusiveAsync(CancellationToken cancellationToken = default)
    {
        snapshot = null;
        return context.BeginExclusiveAsync(cancellationToken);
    }

    public Task<IDbContextTransaction> BeginSharedAsync(CancellationToken cancellationToken = default)
    {
        snapshot = null;
        return context.BeginSharedAsync(cancellationToken);
    }

    /// <summary>
    /// Reads the rows that refer to units and positions before the units and positions themselves: those are never deleted,
    /// so without the tree lock, when changes may commit between the queries, every unit and position referred to is still in the snapshot.
    /// </summary>
    private async Task<OrgSnapshot> ReadOrgSnapshotAsync(CancellationToken cancellationToken)
    {
        var placementCounts = await context
            .WorkingPlacements()
            .GroupBy(placement => new { placement.OrgUnitId, placement.PositionId })
            .Select(group => new { group.Key.OrgUnitId, group.Key.PositionId, Any = group.Count(), Main = group.Count(placement => placement.IsMain) })
            .ToListAsync(cancellationToken);
        var workingEmployeeCounts = placementCounts
            .Where(group => group.Main > 0)
            .ToDictionary(group => (group.OrgUnitId, group.PositionId), group => group.Main);
        var workingAssigneeCounts = placementCounts.ToDictionary(group => (group.OrgUnitId, group.PositionId), group => group.Any);
        var unitPositions = await context
            .Set<OrgUnitPosition>()
            .AsNoTracking()
            .Select(link => new { link.OrgUnitId, link.PositionId })
            .ToListAsync(cancellationToken);
        var positions = await context
            .Set<Position>()
            .AsNoTracking()
            .Select(position => new PositionStatus(position.Id, position.Name, position.IsActive))
            .ToDictionaryAsync(position => position.Id, cancellationToken);
        var units = await context
            .Set<OrgUnit>()
            .AsNoTracking()
            .Select(UnitStatusProjection)
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        return new OrgSnapshot(units, unitPositions.ToLookup(link => link.OrgUnitId, link => link.PositionId), positions, workingEmployeeCounts, workingAssigneeCounts);
    }
}
