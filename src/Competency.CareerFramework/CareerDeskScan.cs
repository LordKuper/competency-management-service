using System.Diagnostics;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;

namespace Competency.CareerFramework;

/// <summary>
/// Works out the career findings of one user's desk in memory from data read beforehand, through the same rule functions the module acts
/// by, so the desk asks the database nothing per object and never disagrees with what an action allows.
/// </summary>
/// <param name="viewer">The user's rights.</param>
/// <param name="snapshot">The organization snapshot.</param>
/// <param name="assigneeOf">Every HR assignment, as the assigned employee by unit.</param>
/// <param name="accounts">The accounts the findings may name, by identifier: the user's, the assigned HR's, the heads' and the authors'.</param>
/// <param name="accountsByEmployee">The accounts bound to the assigned HR employees and to the heads, by employee, when the findings need them.</param>
/// <param name="people">The employees the findings may name, by identifier: the user, the heads, the assigned HR and those bound to the accounts.</param>
/// <param name="headAccountOf">The account of each unit's head who can act, by unit.</param>
/// <param name="tracks">Every track with its levels and links.</param>
/// <param name="versions">Every matrix version in work or published.</param>
/// <param name="lastDecisionOf">The latest decision on each version in work, by version.</param>
/// <param name="now">The moment the desk is worked out at.</param>
internal sealed class CareerDeskScan(
    CatalogViewer viewer,
    OrgSnapshot snapshot,
    IReadOnlyDictionary<Guid, Guid> assigneeOf,
    IReadOnlyDictionary<Guid, UserStatus> accounts,
    IReadOnlyDictionary<Guid, UserStatus> accountsByEmployee,
    IReadOnlyDictionary<Guid, EmployeeStatus> people,
    IReadOnlyDictionary<Guid, Guid> headAccountOf,
    IReadOnlyList<CareerDeskTrack> tracks,
    IReadOnlyList<CareerDeskVersion> versions,
    IReadOnlyDictionary<Guid, CareerDeskDecision> lastDecisionOf,
    DateTimeOffset now)
{
    /// <summary>
    /// How long a version may wait for the superior head before its responsible HR is told.
    /// </summary>
    private static readonly TimeSpan StuckAfter = TimeSpan.FromDays(7);

    private readonly Dictionary<Guid, CareerDeskTrack> trackOf = tracks.ToDictionary(row => row.Track.Id);

    /// <summary>
    /// The units with a track matrix version in work; a position matrix does not count as a unit's matrix.
    /// </summary>
    private readonly HashSet<Guid> inWorkUnitIds =
        [.. versions.Where(version => version.TrackId is not null && version.Status != MatrixVersionStatus.Published).Select(version => version.UnitId)];

    /// <summary>
    /// The units with a published track matrix.
    /// </summary>
    private readonly HashSet<Guid> publishedUnitIds =
        [.. versions.Where(version => version.TrackId is not null && version.Status == MatrixVersionStatus.Published).Select(version => version.UnitId)];

    /// <summary>
    /// The positions with a position matrix of any unit, each of which has a version in work or a published one; no track can take them.
    /// </summary>
    private readonly HashSet<Guid> matrixPositionIds = [.. versions.Select(version => version.PositionId).OfType<Guid>()];

    /// <summary>
    /// The positions linked to an active level of an active track, each with the track's unit.
    /// </summary>
    private readonly HashSet<(Guid UnitId, Guid PositionId)> trackedPositions =
    [
        .. tracks.Where(row => row.Track.Status == CatalogStatus.Active)
            .SelectMany(row => PositionsOfActiveLevels(row).Select(positionId => (row.Track.OwnerOrgUnitId, positionId))),
    ];

    private readonly Dictionary<Guid, Guid?> responsibleHrOf = [];

    private readonly Dictionary<Guid, Guid?> superiorApproverOf = [];

    /// <summary>
    /// The entries nothing uses, for an HR or a global administrator to clear away.
    /// </summary>
    /// <param name="unused">The unused entries.</param>
    /// <returns>One finding per entry, for reference.</returns>
    public static IEnumerable<DeskFinding> UnusedFindings(IEnumerable<UnusedEntryResponse> unused) => unused.Select(entry => entry.Kind switch
    {
        CatalogEntryKind.Competency => new DeskFinding(DeskKind.UnusedCompetency, new DeskObject(DeskObjectType.Competency, entry.Id, entry.Name), IsAction: false),
        CatalogEntryKind.Scale => new DeskFinding(DeskKind.UnusedScale, new DeskObject(DeskObjectType.Scale, entry.Id, entry.Name), IsAction: false),
        CatalogEntryKind.Direction => new DeskFinding(DeskKind.DirectionWithoutTracks, new DeskObject(DeskObjectType.Direction, entry.Id, entry.Name), IsAction: false),
        _ => throw new UnreachableException($"Unused entries are never of kind {entry.Kind}."),
    });

    /// <summary>
    /// Units without a responsible HR who can act, for a global administrator; units the user heads that have no track matrix in force
    /// although an active track applies to them; and, for their responsible HR, units without a track matrix in force or without a head. A
    /// version in work of a track matrix counts as a matrix on its way, whether or not the user is told about the version.
    /// </summary>
    /// <returns>The findings about units.</returns>
    public IEnumerable<DeskFinding> UnitFindings()
    {
        foreach (var unit in snapshot.Units.Values.Where(unit => unit.IsActive))
        {
            if (viewer.IsAdmin && ResponsibleHr(unit.Id) is null)
            {
                yield return WithoutHr(unit);
            }

            var lacksMatrix = !publishedUnitIds.Contains(unit.Id) && !inWorkUnitIds.Contains(unit.Id);
            if (lacksMatrix && viewer.HeadedUnitIds.Contains(unit.Id) && ActiveTracksOver(unit.Id) is { Count: > 0 } activeTracks)
            {
                yield return new DeskFinding(DeskKind.UnitWithoutMatrix, snapshot.UnitDeskObject(unit), IsAction: true, Details: new DeskDetails(Entries: activeTracks));
            }

            if (!viewer.HrUnitIds.Contains(unit.Id))
            {
                continue;
            }

            if (lacksMatrix)
            {
                yield return new DeskFinding(DeskKind.ResponsibleUnitWithoutMatrix, snapshot.UnitDeskObject(unit), IsAction: false, Details: new DeskDetails(Person: HeadPerson(unit)));
            }

            if (unit.HeadEmployeeId is null)
            {
                yield return new DeskFinding(DeskKind.ResponsibleUnitWithoutHead, snapshot.UnitDeskObject(unit), IsAction: false);
            }
        }
    }

    /// <summary>
    /// The versions in work the user reads and has a part in: as a head of the matrix unit, as the approver whose turn it is, or as the
    /// responsible HR of a version waiting too long for the superior head. Every finding is about the matrix and opens the version.
    /// </summary>
    /// <returns>The findings about versions in work.</returns>
    public IEnumerable<DeskFinding> VersionFindings()
    {
        foreach (var version in versions.Where(version => version.Status != MatrixVersionStatus.Published))
        {
            var superior = version is { Status: MatrixVersionStatus.OnReview, Step: ApprovalStep.Superior } ? SuperiorApprover(version.UnitId) : null;
            if (!MatrixRoute.ReadsAnyVersion(viewer, version.UnitId, version.AuthorId) && superior != viewer.UserId)
            {
                continue;
            }

            var turn = MatrixRoute.Turn(version.Status, version.Step, superior, superior is null ? ResponsibleHr(version.UnitId) : null);
            foreach (var finding in VersionFindings(version, turn))
            {
                yield return finding;
            }
        }
    }

    /// <summary>
    /// The published matrices of the units the user heads that need a new version: their track's levels changed, they rely on an inactive
    /// track, direction, position, shared block or competency, or the composition of a shared block changed since; and, for reference,
    /// their competencies changed since. Only matrices without a version in work are given.
    /// </summary>
    /// <param name="published">The published versions, each with its matrix.</param>
    /// <param name="directions">The directions by identifier, covering at least those of the matrices.</param>
    /// <param name="competencies">The competencies by identifier, covering at least those of the versions.</param>
    /// <param name="blocks">The shared blocks as they stand now, covering at least those of the versions.</param>
    /// <returns>The findings about published matrices.</returns>
    public IEnumerable<DeskFinding> PublishedFindings(
        IReadOnlyList<MatrixVersion> published,
        IReadOnlyDictionary<Guid, CareerDirection> directions,
        IReadOnlyDictionary<Guid, CareerCompetency> competencies,
        IReadOnlyDictionary<Guid, CompetencyBlock> blocks)
    {
        foreach (var version in published.Where(version => viewer.HeadedUnitIds.Contains(version.Matrix.OrgUnitId)))
        {
            if (version.Snapshot is not { } entries)
            {
                continue;
            }

            var matrix = MatrixObject(version.MatrixId, version.Matrix.TrackId, version.Matrix.PositionId, version.Matrix.OrgUnitId);
            var target = VersionObject(version.Id, version.Matrix.TrackId, version.Matrix.PositionId);
            var details = new DeskDetails(Number: version.Number);
            if (InactiveEntries(version, entries, directions, competencies, blocks) is { Count: > 0 } inactive)
            {
                yield return new DeskFinding(DeskKind.MatrixUsesInactive, matrix, IsAction: true, Details: details with { Entries = inactive }, Target: target);
            }

            if (version.Matrix.TrackId is { } trackId && trackOf.TryGetValue(trackId, out var row) && LevelChanges(entries.Levels, row.Levels) is { } changes)
            {
                yield return new DeskFinding(DeskKind.MatrixLevelsChanged, matrix, IsAction: true, Details: changes with { Number = version.Number }, Target: target);
            }

            List<DeskObject> changedBlocks =
            [
                .. entries.SharedBlocks
                    .Select(entry => (Entry: entry, Current: blocks.GetValueOrDefault(entry.Id)))
                    .Where(pair => pair.Current is { } current && !current.CompetencyIds.ToHashSet().SetEquals(pair.Entry.CompetencyIds))
                    .Select(pair => new DeskObject(DeskObjectType.Block, pair.Entry.Id, pair.Current!.Name)),
            ];
            if (changedBlocks.Count > 0)
            {
                yield return new DeskFinding(DeskKind.MatrixSharedBlockChanged, matrix, IsAction: true, Details: details with { Entries = changedBlocks }, Target: target);
            }

            List<DeskObject> changed =
            [
                .. entries.Competencies
                    .Select(entry => (Entry: entry, Current: competencies.GetValueOrDefault(entry.Id)))
                    .Where(pair => pair.Current is { } current && CompetencyVersion.NumberOf(current) > pair.Entry.VersionNumber)
                    .Select(pair => new DeskObject(DeskObjectType.Competency, pair.Entry.Id, pair.Current!.Name)),
            ];
            if (changed.Count > 0)
            {
                yield return new DeskFinding(DeskKind.MatrixCompetencyChanged, matrix, IsAction: false, Details: details with { Entries = changed }, Target: target);
            }
        }
    }

    /// <summary>
    /// The tracks of the units the user heads or is the responsible HR of that cannot become or stay ready: without levels, with one level,
    /// or with levels that no active position stands for. An action for whoever may edit the track; for reference, naming the responsible HR, otherwise.
    /// </summary>
    /// <returns>The findings about tracks.</returns>
    public IEnumerable<DeskFinding> TrackFindings()
    {
        foreach (var row in tracks.Where(row => row.Track.Status != CatalogStatus.Inactive && IsResponsibleFor(row.Track.OwnerOrgUnitId)))
        {
            var track = row.Track;
            var readiness = Readiness(row);
            var isAction = viewer.CanEdit(track);
            var details = new DeskDetails(Person: isAction ? null : PersonOf(ResponsibleHr(track.OwnerOrgUnitId)), LevelCount: row.Levels.Count);
            foreach (var kind in TrackKinds(row, readiness))
            {
                yield return new DeskFinding(
                    kind,
                    TrackObject(track),
                    isAction,
                    Details: kind == DeskKind.LevelWithoutPosition
                        ? details with { Entries = [.. readiness.LevelsWithoutActivePosition.Select(LevelObject)] }
                        : details);
            }
        }
    }

    /// <summary>
    /// The active positions on the lists of the units the user heads or is the responsible HR of that no active level of an active track of
    /// the unit or a unit above it stands for and that have no position matrix of any unit, which no track could take. An action when a
    /// track the user may edit can take the position; for reference otherwise.
    /// </summary>
    /// <returns>One finding per position, counting the working employees who hold it under the units concerned.</returns>
    public IEnumerable<DeskFinding> PositionFindings()
    {
        var unitsWithoutTrack = new Dictionary<Guid, List<Guid>>();
        foreach (var unitId in viewer.HeadedUnitIds.Union(viewer.HrUnitIds))
        {
            var ancestorIds = snapshot.Ancestry(unitId).Select(unit => unit.Id).ToList();
            foreach (var positionId in snapshot.UnitPositionIds[unitId]
                         .Where(id => snapshot.Positions.GetValueOrDefault(id)?.IsActive == true && !matrixPositionIds.Contains(id)))
            {
                if (!ancestorIds.Any(ancestorId => trackedPositions.Contains((ancestorId, positionId))))
                {
                    unitsWithoutTrack.TryAdd(positionId, []);
                    unitsWithoutTrack[positionId].Add(unitId);
                }
            }
        }

        if (unitsWithoutTrack.Count == 0)
        {
            yield break;
        }

        HashSet<Guid> editable =
        [
            .. tracks.Where(row => row.Track.Status != CatalogStatus.Inactive && viewer.CanEdit(row.Track))
                .SelectMany(row => CareerLevelPositionEndpoints.AvailablePositions(snapshot, row.Track.OwnerOrgUnitId))
                .Select(position => position.Id),
        ];
        var countsByPosition = snapshot.WorkingEmployeeCounts.ToLookup(pair => pair.Key.PositionId);
        foreach (var (positionId, unitIds) in unitsWithoutTrack)
        {
            var isAction = editable.Contains(positionId);
            yield return new DeskFinding(
                DeskKind.PositionWithoutTrack,
                PositionObject(positionId),
                isAction,
                Details: new DeskDetails(
                    EmployeeCount: EmployeesUnder(countsByPosition[positionId], unitIds),
                    Person: isAction ? null : PersonOf(ResponsibleHr(unitIds[0]))));
        }
    }

    /// <summary>
    /// The drafts the user may approve, marked when a draft track is not ready yet, and the drafts the user wrote, for reference.
    /// </summary>
    /// <param name="drafts">The drafts the user can read, levels aside, since a level is approved with its track.</param>
    /// <returns>The findings about drafts.</returns>
    public IEnumerable<DeskFinding> DraftFindings(IEnumerable<CatalogEntry> drafts)
    {
        foreach (var draft in drafts)
        {
            if (viewer.CanManage(draft))
            {
                yield return new DeskFinding(DeskKind.DraftAwaitsApproval, EntryObject(draft), IsAction: true, OccurredAt: draft.CreatedAt, Details: ApprovalDetails(draft));
            }

            if (draft.AuthorId == viewer.UserId)
            {
                yield return new DeskFinding(DeskKind.OwnDraftAwaitsApproval, EntryObject(draft), IsAction: false, OccurredAt: draft.CreatedAt);
            }
        }
    }

    /// <summary>
    /// Tells a working employee, for reference, that no active level of an active track stands for their position.
    /// </summary>
    /// <param name="self">The employee the user is bound to.</param>
    /// <returns>The finding, or nothing when the position has a track or the employee no longer works.</returns>
    public IEnumerable<DeskFinding> NoTrackFindings(EmployeeStatus self)
    {
        if (!self.IsActive || trackedPositions.Any(pair => pair.PositionId == self.PositionId))
        {
            yield break;
        }

        var unit = snapshot.Units.GetValueOrDefault(self.OrgUnitId);
        yield return new DeskFinding(
            DeskKind.NoTrackForPosition,
            new DeskObject(DeskObjectType.Employee, self.Id, self.FullName, unit is null ? null : [unit.Name]),
            IsAction: false,
            Subject: PositionObject(self.PositionId));
    }

    private static IEnumerable<DeskKind> TrackKinds(CareerDeskTrack row, TrackReadiness readiness)
    {
        if (!readiness.HasLevels)
        {
            yield return DeskKind.TrackWithoutLevels;
            yield break;
        }

        if (row.Levels.Count == 1)
        {
            yield return DeskKind.TrackWithOneLevel;
        }

        if (readiness.LevelsWithoutActivePosition.Count == row.Levels.Count)
        {
            yield return DeskKind.TrackWithoutPositions;
        }
        else if (readiness.LevelsWithoutActivePosition.Count > 0)
        {
            yield return DeskKind.LevelWithoutPosition;
        }
    }

    /// <summary>
    /// What changed in a track's levels since its matrix was published: levels added, levels gone out of use, and levels whose order among
    /// those kept changed.
    /// </summary>
    private static DeskDetails? LevelChanges(IReadOnlyList<MatrixLevelEntry> published, IReadOnlyList<CareerLevel> current)
    {
        var publishedIds = published.Select(level => level.Id).ToList();
        var currentIds = current.Select(level => level.Id).ToList();
        var keptInPublishedOrder = publishedIds.Where(currentIds.Contains).ToList();
        var keptInCurrentOrder = currentIds.Where(publishedIds.Contains).ToList();
        var added = current.Where(level => !publishedIds.Contains(level.Id)).Select(LevelObject).ToList();
        var removed = published.Where(level => !currentIds.Contains(level.Id)).Select(level => new DeskObject(DeskObjectType.Level, level.Id, level.Name)).ToList();
        var moved = current
            .Where(level => keptInCurrentOrder.IndexOf(level.Id) is var index and >= 0 && keptInPublishedOrder[index] != level.Id)
            .Select(LevelObject)
            .ToList();
        return added.Count + removed.Count + moved.Count == 0
            ? null
            : new DeskDetails(AddedLevels: NullIfEmpty(added), RemovedLevels: NullIfEmpty(removed), MovedLevels: NullIfEmpty(moved));
    }

    private static IEnumerable<Guid> PositionsOfActiveLevels(CareerDeskTrack row)
    {
        var activeLevelIds = row.Levels.Where(level => level.Status == CatalogStatus.Active).Select(level => level.Id).ToHashSet();
        return row.Links.Where(link => activeLevelIds.Contains(link.LevelId)).Select(link => link.PositionId);
    }

    private static DeskObjectType TypeOf(CatalogEntry entry) => entry switch
    {
        CareerDirection => DeskObjectType.Direction,
        CareerTrack => DeskObjectType.Track,
        CareerLevel => DeskObjectType.Level,
        CareerCompetency => DeskObjectType.Competency,
        CompetencyScale => DeskObjectType.Scale,
        CompetencyBlock => DeskObjectType.Block,
        _ => throw new UnreachableException($"Unknown catalogue entry type {entry.GetType().Name}."),
    };

    private static DeskObject LevelObject(CareerLevel level) => new(DeskObjectType.Level, level.Id, level.Name);

    private static List<DeskObject>? NullIfEmpty(List<DeskObject> objects) => objects.Count == 0 ? null : objects;

    private IEnumerable<DeskFinding> VersionFindings(CareerDeskVersion version, MatrixTurn? turn)
    {
        var last = lastDecisionOf.GetValueOrDefault(version.Id);
        var decidedAt = last?.At ?? version.CreatedAt;
        var matrix = MatrixObject(version.MatrixId, version.TrackId, version.PositionId, version.UnitId);
        var target = VersionObject(version.Id, version.TrackId, version.PositionId);
        var details = new DeskDetails(Number: version.Number);
        var isHead = viewer.HeadedUnitIds.Contains(version.UnitId);
        if (version.Status == MatrixVersionStatus.Returned && isHead)
        {
            yield return new DeskFinding(
                DeskKind.VersionReturned,
                matrix,
                IsAction: true,
                OccurredAt: decidedAt,
                Details: details with { HasChanges = last?.HasChanges == true, HasComment = last?.HasComment == true },
                Target: target);
        }

        if (version.Status == MatrixVersionStatus.Draft && isHead)
        {
            yield return new DeskFinding(DeskKind.VersionNotSubmitted, matrix, IsAction: true, OccurredAt: decidedAt, Details: details with { Step = version.BuilderStep }, Target: target);
        }

        if (turn is null)
        {
            yield break;
        }

        var onStep = details with { Step = turn.Step == ApprovalStep.Superior ? 1 : 2 };
        if (turn.ApproverId == viewer.UserId)
        {
            yield return new DeskFinding(
                turn.Step == ApprovalStep.Superior ? DeskKind.VersionAwaitsHeadApproval : DeskKind.VersionAwaitsHrApproval,
                matrix,
                IsAction: true,
                OccurredAt: decidedAt,
                Details: onStep with { Person = PersonOf(version.AuthorId) },
                Target: target);
        }

        var withApprover = onStep with { Person = PersonOf(turn.ApproverId) };
        if (isHead)
        {
            yield return new DeskFinding(DeskKind.OwnVersionInApproval, matrix, IsAction: false, OccurredAt: decidedAt, Details: withApprover, Target: target);
        }

        if (turn.Step == ApprovalStep.Superior && viewer.HrUnitIds.Contains(version.UnitId) && now - decidedAt > StuckAfter)
        {
            yield return new DeskFinding(DeskKind.VersionStuckAtHead, matrix, IsAction: false, OccurredAt: decidedAt, Details: withApprover, Target: target);
        }
    }

    /// <summary>
    /// The finding about a unit without a responsible HR who can act, naming the assigned employee and why the assignment does not act, if one is assigned.
    /// </summary>
    private DeskFinding WithoutHr(OrgUnitStatus unit)
    {
        Guid? assigneeId = ProcessRoles.NearestAssigned(snapshot.Ancestry(unit.Id), assigneeOf) is { } nearest ? assigneeOf[nearest.Id] : null;
        DeskAccountState? state = assigneeId is { } id
            ? accountsByEmployee.GetValueOrDefault(id) switch
            {
                null => DeskAccountState.None,
                { IsBlocked: true } => DeskAccountState.Blocked,
                { IsRegistered: false } => DeskAccountState.Invited,
                _ => DeskAccountState.NoHrRole,
            }
            : null;
        var person = assigneeId is { } employeeId && people.GetValueOrDefault(employeeId) is { } assignee
            ? new DeskObject(DeskObjectType.Employee, assignee.Id, assignee.FullName)
            : null;
        return new DeskFinding(DeskKind.UnitWithoutHr, snapshot.UnitDeskObject(unit), IsAction: true, Details: new DeskDetails(Person: person, AccountState: state));
    }

    private List<DeskObject> InactiveEntries(
        MatrixVersion version,
        MatrixEntries entries,
        IReadOnlyDictionary<Guid, CareerDirection> directions,
        IReadOnlyDictionary<Guid, CareerCompetency> competencies,
        IReadOnlyDictionary<Guid, CompetencyBlock> blocks)
    {
        List<DeskObject> inactive = [];
        if (version.Matrix.TrackId is { } trackId && trackOf.GetValueOrDefault(trackId)?.Track.Status == CatalogStatus.Inactive)
        {
            inactive.Add(new DeskObject(DeskObjectType.Track, trackId, entries.Track?.Name ?? TrackName(trackId)));
        }

        if (version.Matrix.DirectionId is { } directionId && directions.GetValueOrDefault(directionId) is { Status: CatalogStatus.Inactive } direction)
        {
            inactive.Add(new DeskObject(DeskObjectType.Direction, directionId, entries.Direction?.Name ?? direction.Name));
        }

        if (version.Matrix.PositionId is { } positionId && snapshot.Positions.GetValueOrDefault(positionId) is { IsActive: false } position)
        {
            inactive.Add(new DeskObject(DeskObjectType.Position, positionId, entries.Position?.Name ?? position.Name));
        }

        inactive.AddRange(entries.SharedBlocks
            .Where(entry => blocks.GetValueOrDefault(entry.Id)?.Status == CatalogStatus.Inactive)
            .Select(entry => new DeskObject(DeskObjectType.Block, entry.Id, entry.Name)));
        inactive.AddRange(entries.Competencies
            .Where(entry => competencies.GetValueOrDefault(entry.Id)?.Status == CatalogStatus.Inactive)
            .Select(entry => new DeskObject(DeskObjectType.Competency, entry.Id, entry.Name)));
        return inactive;
    }

    private DeskDetails ApprovalDetails(CatalogEntry draft)
    {
        var details = new DeskDetails(Person: PersonOf(draft.AuthorId));
        if (draft is not CareerTrack track || !trackOf.TryGetValue(track.Id, out var row))
        {
            return details;
        }

        return details with
        {
            LevelCount = row.Levels.Count,
            Entries = NullIfEmpty([.. Readiness(row).LevelsWithoutActivePosition.Select(LevelObject)]),
        };
    }

    private List<DeskObject> ActiveTracksOver(Guid unitId)
    {
        var ancestorIds = snapshot.Ancestry(unitId).Select(unit => unit.Id).ToHashSet();
        return
        [
            .. tracks.Where(row => row.Track.Status == CatalogStatus.Active && ancestorIds.Contains(row.Track.OwnerOrgUnitId))
                .Select(row => TrackObject(row.Track))
                .OrderBy(track => track.Name, TextCollation.Comparer),
        ];
    }

    private int EmployeesUnder(IEnumerable<KeyValuePair<(Guid OrgUnitId, Guid PositionId), int>> counts, List<Guid> unitIds)
    {
        var topIds = unitIds.ToHashSet();
        return counts.Where(pair => snapshot.Ancestry(pair.Key.OrgUnitId).Any(unit => topIds.Contains(unit.Id))).Sum(pair => pair.Value);
    }

    private bool IsResponsibleFor(Guid unitId) => viewer.HeadedUnitIds.Contains(unitId) || viewer.HrUnitIds.Contains(unitId);

    private TrackReadiness Readiness(CareerDeskTrack row) =>
        TrackReadiness.Of(row.Levels, row.Links.ToLookup(link => link.LevelId, link => link.PositionId), snapshot.Positions);

    private Guid? ResponsibleHr(Guid unitId) => responsibleHrOf.TryGetValue(unitId, out var hrId)
        ? hrId
        : responsibleHrOf[unitId] = ProcessRoles.ResponsibleHr(snapshot.Ancestry(unitId), assigneeOf, accountsByEmployee);

    private Guid? SuperiorApprover(Guid unitId) => superiorApproverOf.TryGetValue(unitId, out var approverId)
        ? approverId
        : superiorApproverOf[unitId] = MatrixRoute.SuperiorApprover(snapshot.Ancestry(unitId), headAccountOf);

    private DeskObject? PersonOf(Guid? accountId) => accountId is { } id && accounts.GetValueOrDefault(id) is { } account
        ? account.EmployeeId is { } employeeId && people.GetValueOrDefault(employeeId) is { } employee
            ? new DeskObject(DeskObjectType.Employee, employee.Id, employee.FullName)
            : new DeskObject(DeskObjectType.UserAccount, account.Id, account.Email)
        : null;

    private DeskObject? HeadPerson(OrgUnitStatus unit) => unit.HeadEmployeeId is { } headId && people.GetValueOrDefault(headId) is { } head
        ? new DeskObject(DeskObjectType.Employee, head.Id, head.FullName)
        : null;

    private DeskObject MatrixObject(Guid matrixId, Guid? trackId, Guid? positionId, Guid unitId) =>
        new(DeskObjectType.Matrix, matrixId, MatrixName(trackId, positionId), UnitPath(unitId));

    private DeskObject VersionObject(Guid versionId, Guid? trackId, Guid? positionId) => new(DeskObjectType.MatrixVersion, versionId, MatrixName(trackId, positionId));

    /// <summary>
    /// A matrix goes by its track's name or, for a position matrix, its position's.
    /// </summary>
    private string MatrixName(Guid? trackId, Guid? positionId) => trackId is { } id
        ? TrackName(id)
        : positionId is { } position ? snapshot.Positions.GetValueOrDefault(position)?.Name ?? string.Empty : string.Empty;

    private DeskObject TrackObject(CareerTrack track) => new(DeskObjectType.Track, track.Id, track.Name, UnitPath(track.OwnerOrgUnitId));

    private DeskObject EntryObject(CatalogEntry entry) => entry is CareerTrack track ? TrackObject(track) : new(TypeOf(entry), entry.Id, entry.Name);

    private DeskObject PositionObject(Guid positionId) =>
        new(DeskObjectType.Position, positionId, snapshot.Positions.GetValueOrDefault(positionId)?.Name ?? string.Empty);

    private string TrackName(Guid trackId) => trackOf.GetValueOrDefault(trackId)?.Track.Name ?? string.Empty;

    private List<string> UnitPath(Guid unitId) => [.. snapshot.Ancestry(unitId).Reverse().Select(unit => unit.Name)];
}
