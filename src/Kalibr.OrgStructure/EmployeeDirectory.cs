using System.Linq.Expressions;
using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kalibr.OrgStructure;

/// <summary>
/// Answers <see cref="IEmployeeDirectory"/> lookups straight from the employee and unit tables.
/// </summary>
internal sealed class EmployeeDirectory(AppDbContext context) : IEmployeeDirectory
{
    private static readonly Expression<Func<Employee, EmployeeStatus>> StatusProjection = employee => new EmployeeStatus(
        employee.Id, employee.FullName, employee.LastName, employee.FirstName, employee.MiddleName, employee.IsActive,
        employee.OrgUnitId, employee.PositionId, employee.Education);

    private static readonly Expression<Func<OrgUnit, OrgUnitStatus>> UnitStatusProjection = unit => new OrgUnitStatus(
        unit.Id, unit.Name, unit.ParentId, unit.HeadEmployeeId, unit.IsActive);

    private OrgSnapshot? snapshot;

    public Task<EmployeeStatus?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default) => context
        .Set<Employee>()
        .AsNoTracking()
        .Where(employee => employee.Id == employeeId)
        .Select(StatusProjection)
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> SetEducationAsync(Guid employeeId, EducationLevel? education, CancellationToken cancellationToken = default)
    {
        var employee = await context.Set<Employee>().FirstOrDefaultAsync(candidate => candidate.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return false;
        }

        employee.Education = education;
        return true;
    }

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

    public async Task<IReadOnlyList<Guid>> FindWorkingInUnitsAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken = default)
    {
        var ids = unitIds.ToArray();
        return await context
            .Set<Employee>()
            .AsNoTracking()
            .Where(employee => employee.IsActive && ids.Contains(employee.OrgUnitId))
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);
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
    /// Reads the rows that refer to units and positions before the units and positions themselves, so without the tree lock, when changes may
    /// commit between the queries, every unit and position created meanwhile and referred to is in the snapshot. Positions are never deleted;
    /// an empty unit may be, and the rows still referring to it are dropped.
    /// </summary>
    private async Task<OrgSnapshot> ReadOrgSnapshotAsync(CancellationToken cancellationToken)
    {
        var placementCounts = await context
            .WorkingPlacements()
            .GroupBy(placement => new { placement.OrgUnitId, placement.PositionId })
            .Select(group => new { group.Key.OrgUnitId, group.Key.PositionId, Any = group.Count(), Main = group.Count(placement => placement.IsMain) })
            .ToListAsync(cancellationToken);
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
        var knownCounts = placementCounts.Where(group => units.ContainsKey(group.OrgUnitId)).ToList();
        var workingEmployeeCounts = knownCounts
            .Where(group => group.Main > 0)
            .ToDictionary(group => (group.OrgUnitId, group.PositionId), group => group.Main);
        var workingAssigneeCounts = knownCounts.ToDictionary(group => (group.OrgUnitId, group.PositionId), group => group.Any);
        var knownLinks = unitPositions.Where(link => units.ContainsKey(link.OrgUnitId));
        return new OrgSnapshot(units, knownLinks.ToLookup(link => link.OrgUnitId, link => link.PositionId), positions, workingEmployeeCounts, workingAssigneeCounts);
    }
}
