namespace Kalibr.OrgStructure;

/// <summary>
/// The whole organization structure read at once, with the module's own tree and position rules, so that callers checking many
/// units at a time neither query per unit nor repeat the rules. A class rather than a record, because the child index it builds
/// from <see cref="Units"/> would go stale in a copy made with different units.
/// </summary>
public sealed class OrgSnapshot
{
    private readonly ILookup<Guid?, OrgUnitStatus> children;

    /// <summary>
    /// Creates a snapshot from the rows read.
    /// </summary>
    /// <param name="units">Every unit, active or not, by identifier.</param>
    /// <param name="unitPositionIds">The positions on each unit's own list, by unit identifier.</param>
    /// <param name="positions">Every catalogue position, active or not, by identifier.</param>
    /// <param name="workingEmployeeCounts">The number of working employees by the unit and position of their main assignment; pairs without any are absent.</param>
    /// <param name="workingAssigneeCounts">The number of working employees by unit and position of any of their assignments, the main and the secondary ones; pairs without any are absent.</param>
    public OrgSnapshot(
        IReadOnlyDictionary<Guid, OrgUnitStatus> units,
        ILookup<Guid, Guid> unitPositionIds,
        IReadOnlyDictionary<Guid, PositionStatus> positions,
        IReadOnlyDictionary<(Guid OrgUnitId, Guid PositionId), int> workingEmployeeCounts,
        IReadOnlyDictionary<(Guid OrgUnitId, Guid PositionId), int> workingAssigneeCounts)
    {
        Units = units;
        UnitPositionIds = unitPositionIds;
        Positions = positions;
        WorkingEmployeeCounts = workingEmployeeCounts;
        WorkingAssigneeCounts = workingAssigneeCounts;
        children = units.Values.ToLookup(unit => unit.ParentId);
    }

    /// <summary>
    /// Every unit, active or not, by identifier.
    /// </summary>
    public IReadOnlyDictionary<Guid, OrgUnitStatus> Units { get; }

    /// <summary>
    /// The positions on each unit's own list, without those it inherits; empty for a unit with no list of its own.
    /// </summary>
    public ILookup<Guid, Guid> UnitPositionIds { get; }

    /// <summary>
    /// Every catalogue position, active or not, by identifier.
    /// </summary>
    public IReadOnlyDictionary<Guid, PositionStatus> Positions { get; }

    /// <summary>
    /// The number of working employees by the unit and position of their main assignment, the only one the rules about matrices, "my place" and the like read;
    /// pairs without any are absent.
    /// </summary>
    public IReadOnlyDictionary<(Guid OrgUnitId, Guid PositionId), int> WorkingEmployeeCounts { get; }

    /// <summary>
    /// The number of working employees by unit and position of any of their assignments, the main and the secondary ones, for what the organization structure
    /// itself reports; pairs without any are absent. An employee with two positions in one unit counts under each.
    /// </summary>
    public IReadOnlyDictionary<(Guid OrgUnitId, Guid PositionId), int> WorkingAssigneeCounts { get; }

    /// <summary>
    /// A unit and every unit above it, for rules that apply to the nearest unit up the tree that meets a condition.
    /// </summary>
    /// <param name="unitId">The unit the chain starts from.</param>
    /// <returns>The unit first, then its parent and so on up to the root; empty when the unit does not exist.</returns>
    public IReadOnlyList<OrgUnitStatus> Ancestry(Guid unitId)
    {
        var chain = new List<OrgUnitStatus>();
        var seen = new HashSet<Guid>();
        Guid? id = unitId;
        while (id is { } current && seen.Add(current) && Units.TryGetValue(current, out var unit))
        {
            chain.Add(unit);
            id = unit.ParentId;
        }

        return chain;
    }

    /// <summary>
    /// A unit and every unit below it, for rules a unit passes down to the units under it.
    /// </summary>
    /// <param name="unitId">The top of the subtree.</param>
    /// <returns>The units of the subtree, active or not, top first; empty when the unit does not exist.</returns>
    public IReadOnlyList<OrgUnitStatus> Subtree(Guid unitId)
    {
        if (!Units.TryGetValue(unitId, out var top))
        {
            return [];
        }

        var subtree = new List<OrgUnitStatus> { top };
        var seen = new HashSet<Guid> { unitId };
        for (var index = 0; index < subtree.Count; index++)
        {
            subtree.AddRange(children[subtree[index].Id].Where(child => seen.Add(child.Id)));
        }

        return subtree;
    }

    /// <summary>
    /// The positions an employee of the unit may hold: those on its own list and on the lists of the units above it.
    /// Whether each is still active is for the caller to check in <see cref="Positions"/>.
    /// </summary>
    /// <param name="unitId">The unit.</param>
    /// <returns>The position identifiers; empty when the unit does not exist.</returns>
    public IReadOnlySet<Guid> AvailablePositionIds(Guid unitId) =>
        Ancestry(unitId).SelectMany(unit => UnitPositionIds[unit.Id]).ToHashSet();
}
