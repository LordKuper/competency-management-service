using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure.Import;

internal sealed record SnapshotUnit(Guid Id, string Name, Guid? ParentId, Guid? HeadEmployeeId, bool IsActive);

internal sealed record SnapshotPosition(Guid Id, string Name, bool IsActive);

internal sealed record SnapshotLegalEntity(Guid Id, string Name);

internal sealed record SnapshotEmployee(Guid Id, string LastName, string FirstName, string? MiddleName, bool IsActive, Guid OrgUnitId, Guid PositionId)
{
    public PersonName Name => new(LastName, FirstName, MiddleName);

    public string FullName => string.Join(' ', new[] { LastName, FirstName, MiddleName }.Where(part => part is not null));
}

internal sealed record SnapshotAssignment(Guid Id, Guid EmployeeId, Guid OrgUnitId, Guid PositionId);

/// <summary>
/// The part of the organization an import is compared with, read once in a fixed number of queries so that planning a file of any size
/// neither queries per row nor per employee. Plain values, never tracked.
/// </summary>
internal sealed class ImportSnapshot
{
    public required IReadOnlyList<SnapshotUnit> Units { get; init; }

    public required IReadOnlyList<SnapshotPosition> Positions { get; init; }

    public required IReadOnlyList<SnapshotLegalEntity> LegalEntities { get; init; }

    public required IReadOnlyList<SnapshotEmployee> Employees { get; init; }

    public required ILookup<Guid, SnapshotAssignment> AssignmentsByEmployee { get; init; }

    /// <summary>
    /// The positions on each unit's own list.
    /// </summary>
    public required ILookup<Guid, Guid> PositionIdsByUnit { get; init; }

    /// <summary>
    /// The legal entities of each employee's main assignment.
    /// </summary>
    public required ILookup<Guid, Guid> MainLegalEntityIds { get; init; }

    /// <summary>
    /// The legal entities of each secondary assignment, by the assignment.
    /// </summary>
    public required ILookup<Guid, Guid> AssignmentLegalEntityIds { get; init; }

    /// <summary>
    /// The employees an account is bound to.
    /// </summary>
    public required IReadOnlySet<Guid> EmployeesWithAccount { get; init; }

    /// <summary>
    /// The path of every unit from its root, the names joined by a slash.
    /// </summary>
    public IReadOnlyDictionary<Guid, string> UnitPaths => paths ??= ComputePaths();

    private Dictionary<Guid, string>? paths;

    /// <summary>
    /// Reads the snapshot; call it under the tree lock so that it is what the apply then changes.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The snapshot.</returns>
    public static async Task<ImportSnapshot> LoadAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var units = await context.Set<OrgUnit>().AsNoTracking()
            .Select(unit => new SnapshotUnit(unit.Id, unit.Name, unit.ParentId, unit.HeadEmployeeId, unit.IsActive))
            .ToListAsync(cancellationToken);
        var positions = await context.Set<Position>().AsNoTracking()
            .Select(position => new SnapshotPosition(position.Id, position.Name, position.IsActive))
            .ToListAsync(cancellationToken);
        var legalEntities = await context.Set<LegalEntity>().AsNoTracking()
            .Select(entity => new SnapshotLegalEntity(entity.Id, entity.Name))
            .ToListAsync(cancellationToken);
        var employees = await context.Set<Employee>().AsNoTracking()
            .Select(employee => new SnapshotEmployee(employee.Id, employee.LastName, employee.FirstName, employee.MiddleName, employee.IsActive, employee.OrgUnitId, employee.PositionId))
            .ToListAsync(cancellationToken);
        var assignments = await context.Set<EmployeeAssignment>().AsNoTracking()
            .Select(assignment => new SnapshotAssignment(assignment.Id, assignment.EmployeeId, assignment.OrgUnitId, assignment.PositionId))
            .ToListAsync(cancellationToken);
        var unitPositions = await context.Set<OrgUnitPosition>().AsNoTracking()
            .Select(link => new { link.OrgUnitId, link.PositionId })
            .ToListAsync(cancellationToken);
        var mainEntities = await context.Set<EmployeeLegalEntity>().AsNoTracking()
            .Select(link => new { link.EmployeeId, link.LegalEntityId })
            .ToListAsync(cancellationToken);
        var assignmentEntities = await context.Set<AssignmentLegalEntity>().AsNoTracking()
            .Select(link => new { link.AssignmentId, link.LegalEntityId })
            .ToListAsync(cancellationToken);
        var accountEmployeeIds = await context.Set<EmployeeAccount>().AsNoTracking()
            .Select(account => account.EmployeeId)
            .ToListAsync(cancellationToken);

        return new ImportSnapshot
        {
            Units = units,
            Positions = positions,
            LegalEntities = legalEntities,
            Employees = employees,
            AssignmentsByEmployee = assignments.ToLookup(assignment => assignment.EmployeeId),
            PositionIdsByUnit = unitPositions.ToLookup(link => link.OrgUnitId, link => link.PositionId),
            MainLegalEntityIds = mainEntities.ToLookup(link => link.EmployeeId, link => link.LegalEntityId),
            AssignmentLegalEntityIds = assignmentEntities.ToLookup(link => link.AssignmentId, link => link.LegalEntityId),
            EmployeesWithAccount = accountEmployeeIds.ToHashSet(),
        };
    }

    private Dictionary<Guid, string> ComputePaths()
    {
        var byId = Units.ToDictionary(unit => unit.Id);
        var result = new Dictionary<Guid, string>();

        string PathOf(SnapshotUnit unit) => result.TryGetValue(unit.Id, out var known)
            ? known
            : result[unit.Id] = unit.ParentId is { } parentId && byId.TryGetValue(parentId, out var parent) ? $"{PathOf(parent)} / {unit.Name}" : unit.Name;

        foreach (var unit in Units)
        {
            PathOf(unit);
        }

        return result;
    }
}
