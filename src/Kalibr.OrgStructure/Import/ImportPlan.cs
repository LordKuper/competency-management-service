namespace Competency.OrgStructure.Import;

/// <summary>
/// A unit of the file's tree, matched with an existing unit or to be created. Its id is the existing one, or the new one once the apply has created it.
/// </summary>
internal sealed class UnitNode
{
    private const string PathSeparator = " / ";

    public required string Name { get; init; }

    public required string PathKey { get; init; }

    public UnitNode? Parent { get; init; }

    public Guid? Id { get; set; }

    public bool IsNew { get; init; }

    public bool WasInactive { get; init; }

    /// <summary>
    /// Whether some row ends in this unit, so that the file states the unit's own position list; a unit that only appears above others keeps its list.
    /// </summary>
    public bool IsDeclared { get; set; }

    /// <summary>
    /// The positions of the rows that end in this unit, which become its own list.
    /// </summary>
    public HashSet<PositionRef> Positions { get; } = [];

    public string PathText => Parent is null ? Name : $"{Parent.PathText}{PathSeparator}{Name}";
}

/// <summary>
/// A position of the file, matched with a catalogue position or to be created.
/// </summary>
internal sealed class PositionRef
{
    public required string Name { get; init; }

    public Guid? Id { get; set; }

    public bool IsNew { get; init; }

    public bool WasInactive { get; init; }
}

/// <summary>
/// A legal entity of the file, matched with a directory entry or to be created.
/// </summary>
internal sealed class LegalEntityRef
{
    public required string Name { get; init; }

    public Guid? Id { get; set; }

    public bool IsNew { get; init; }
}

/// <summary>
/// A place an employee works at, as the file states it.
/// </summary>
/// <param name="Unit">The unit.</param>
/// <param name="Position">The position.</param>
internal sealed record Placement(UnitNode Unit, PositionRef Position)
{
    public override string ToString() => $"{Unit.PathText}, {Position.Name}";

    public bool Is(Guid unitId, Guid positionId) => Unit.Id == unitId && Position.Id == positionId;
}

/// <summary>
/// What the import does to one employee of the file: create them, or bring them to the file's assignments, legal entities and working status.
/// </summary>
internal sealed class EmployeeChange
{
    public required PersonName Name { get; init; }

    public SnapshotEmployee? Existing { get; init; }

    public required Placement Main { get; init; }

    public required IReadOnlyList<Placement> Secondary { get; init; }

    public required IReadOnlyDictionary<Placement, List<LegalEntityRef>> LegalEntities { get; init; }

    public bool IsRehire { get; init; }

    public bool IsTransfer { get; init; }

    public IReadOnlyList<Placement> AddedAssignments { get; init; } = [];

    public IReadOnlyList<SnapshotAssignment> RemovedAssignments { get; init; } = [];

    public bool MainLegalEntitiesChanged { get; init; }

    public IReadOnlyList<Placement> ChangedAssignmentLegalEntities { get; init; } = [];

    public string FullName => Existing?.FullName ?? string.Join(' ', new[] { Name.LastName, Name.FirstName, Name.MiddleName }.Where(part => part is not null));

    /// <summary>
    /// The key the head step names the employee by: the identifier of an existing employee, the normalised full name of a new one.
    /// </summary>
    public string Key => Existing is null ? Name.Key : Existing.Id.ToString();

    /// <summary>
    /// Every place the employee works at once the file is applied, the main one first.
    /// </summary>
    public IEnumerable<Placement> Placements => Secondary.Prepend(Main);

    public bool HasChanges =>
        Existing is null || IsRehire || IsTransfer || AddedAssignments.Count > 0 || RemovedAssignments.Count > 0
        || MainLegalEntitiesChanged || ChangedAssignmentLegalEntities.Count > 0;
}

/// <summary>
/// The positions added to and taken off the own list of one unit of the file.
/// </summary>
/// <param name="Unit">The unit.</param>
/// <param name="Added">The positions put on the list.</param>
/// <param name="RemovedPositionIds">The catalogue positions taken off the list.</param>
internal sealed record UnitListChange(UnitNode Unit, IReadOnlyList<PositionRef> Added, IReadOnlyList<Guid> RemovedPositionIds);

/// <summary>
/// Everything an import would do, computed from the file and the organization as it is now: the same value is shown by the preview
/// and applied by the apply, which computes it again under the tree lock.
/// </summary>
internal sealed class ImportPlan
{
    public required ImportSnapshot Snapshot { get; init; }

    /// <summary>
    /// Every unit of the file's tree, each after its parent.
    /// </summary>
    public required IReadOnlyList<UnitNode> Units { get; init; }

    public required IReadOnlyList<PositionRef> Positions { get; init; }

    public required IReadOnlyList<LegalEntityRef> LegalEntities { get; init; }

    public required IReadOnlyList<EmployeeChange> Employees { get; init; }

    public required IReadOnlyList<UnitListChange> UnitLists { get; init; }

    public required IReadOnlyList<MissingUnitResponse> MissingUnits { get; init; }

    public required IReadOnlyList<MissingEmployeeResponse> MissingEmployees { get; init; }

    /// <summary>
    /// The plan as the preview shows it, without the fingerprint.
    /// </summary>
    /// <returns>The groups and the lists of missing units and employees, each in a fixed order.</returns>
    public OrgImportPlanResponse ToResponse()
    {
        var unitPaths = Snapshot.UnitPaths;
        var positionNames = Snapshot.Positions.ToDictionary(position => position.Id, position => position.Name);
        var changes = Employees.Where(change => change.HasChanges).ToList();
        var existing = changes.Where(change => change.Existing is not null).ToList();

        string Described(Guid unitId, Guid positionId) => $"{unitPaths[unitId]}, {positionNames[positionId]}";

        return new OrgImportPlanResponse(
            Fingerprint: string.Empty,
            NewUnits: Sorted(Units.Where(unit => unit.IsNew).Select(unit => unit.PathText)),
            ActivatedUnits: Sorted(Units.Where(unit => !unit.IsNew && unit.WasInactive).Select(unit => unit.PathText)),
            NewPositions: Sorted(Positions.Where(position => position.IsNew).Select(position => position.Name)),
            ReactivatedPositions: Sorted(Positions.Where(position => position.WasInactive).Select(position => position.Name)),
            NewLegalEntities: Sorted(LegalEntities.Where(entity => entity.IsNew).Select(entity => entity.Name)),
            NewEmployees: Sorted(changes.Where(change => change.Existing is null).Select(change => $"{change.FullName}: {change.Main}")),
            RehiredEmployees: Sorted(existing.Where(change => change.IsRehire).Select(change => change.FullName)),
            Transfers: Sorted(existing.Where(change => change.IsTransfer).Select(change =>
                $"{change.FullName}: {Described(change.Existing!.OrgUnitId, change.Existing.PositionId)} → {change.Main}")),
            AddedAssignments: Sorted(changes.SelectMany(change => change.AddedAssignments.Select(placement => $"{change.FullName}: {placement}"))),
            RemovedAssignments: Sorted(existing.SelectMany(change => change.RemovedAssignments
                .Where(assignment => !change.Main.Is(assignment.OrgUnitId, assignment.PositionId))
                .Select(assignment => $"{change.FullName}: {Described(assignment.OrgUnitId, assignment.PositionId)}"))),
            ChangedLegalEntities: Sorted(existing.SelectMany(LegalEntityChanges)),
            UnitPositionsAdded: Sorted(UnitLists.SelectMany(list => list.Added.Select(position => $"{list.Unit.PathText}: {position.Name}"))),
            UnitPositionsRemoved: Sorted(UnitLists.SelectMany(list => list.RemovedPositionIds.Select(id => $"{list.Unit.PathText}: {positionNames[id]}"))),
            MissingUnits: MissingUnits,
            MissingEmployees: MissingEmployees,
            Heads: ImportHeads.Describe(this));
    }

    private static IReadOnlyList<string> Sorted(IEnumerable<string> lines) => [.. lines.Order(StringComparer.Ordinal)];

    private IEnumerable<string> LegalEntityChanges(EmployeeChange change)
    {
        var current = Snapshot.LegalEntities.ToDictionary(entity => entity.Id, entity => entity.Name);
        string Names(IEnumerable<string> names) => names.Any() ? string.Join(", ", names.Order(StringComparer.Ordinal)) : "нет";
        string Line(Placement placement, IEnumerable<Guid> was) =>
            $"{change.FullName}, {placement}: {Names(was.Select(id => current[id]))} → {Names(change.LegalEntities[placement].Select(entity => entity.Name))}";

        if (change.MainLegalEntitiesChanged)
        {
            yield return Line(change.Main, Snapshot.MainLegalEntityIds[change.Existing!.Id]);
        }

        foreach (var placement in change.ChangedAssignmentLegalEntities)
        {
            var assignment = Snapshot.AssignmentsByEmployee[change.Existing!.Id].Single(candidate => placement.Is(candidate.OrgUnitId, candidate.PositionId));
            yield return Line(placement, Snapshot.AssignmentLegalEntityIds[assignment.Id]);
        }
    }
}
