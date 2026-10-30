using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// What the career model expects of the signed-in user, or tells them for reference, by their roles at the moment of asking. Data is read
/// in a fixed number of set-based queries whatever the size of the organization, and only what the user's roles call for; the rules then
/// run in memory, through <see cref="CareerDeskScan"/>.
/// </summary>
internal sealed class CareerDeskSource(
    ICurrentActor actor,
    AppDbContext context,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ProcessRoles roles,
    CatalogStore store,
    TimeProvider time) : IDeskSource
{
    /// <inheritdoc />
    public async Task<DeskReport> FindAsync(CancellationToken cancellationToken)
    {
        if (actor.UserId is not { } userId)
        {
            return new DeskReport([], []);
        }

        if (await users.FindAsync(userId, cancellationToken) is not { IsActive: true } account)
        {
            return new DeskReport([], []);
        }

        var assigneeOf = await roles.AllAssigneesAsync(cancellationToken);
        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        var hrUnitIds = ProcessRoles.HrUnits(snapshot, assigneeOf, account).Select(unit => unit.Id).ToHashSet();
        var mayHead = account.EmployeeId is { } employeeId && snapshot.Units.Values.Any(unit => unit.HeadEmployeeId == employeeId);
        List<Guid> headIds = mayHead || hrUnitIds.Count > 0
            ? [.. snapshot.Units.Values.Where(unit => unit.IsActive).Select(unit => unit.HeadEmployeeId).OfType<Guid>().Distinct()]
            : [];
        var people = new Dictionary<Guid, EmployeeStatus>();
        List<Guid> namedIds = [.. headIds, .. assigneeOf.Values];
        if (account.EmployeeId is { } ownId)
        {
            namedIds.Add(ownId);
        }

        await AddEmployeesAsync(namedIds, people, cancellationToken);
        var self = account.EmployeeId is { } selfId ? people.GetValueOrDefault(selfId) : null;
        var headedUnits = ProcessRoles.HeadedUnits(snapshot.Units.Values, account, self).ToList();
        var viewer = new CatalogViewer(
            userId,
            actor.IsGlobalAdmin(),
            actor.IsHr(),
            headedUnits.Select(unit => unit.Id).ToHashSet(),
            ProcessRoles.MatrixUnits(headedUnits, snapshot.Units.Values.ToLookup(unit => unit.ParentId)).Select(unit => unit.Id).ToHashSet(),
            hrUnitIds);
        var isInvolved = viewer.HeadedUnitIds.Count > 0 || viewer.HrUnitIds.Count > 0;

        var accountsByEmployee = await AccountsByEmployeeAsync(
            [.. viewer.IsAdmin || isInvolved ? assigneeOf.Values : [], .. isInvolved ? headIds : []],
            cancellationToken);
        var accounts = new Dictionary<Guid, UserStatus> { [account.Id] = account };
        foreach (var bound in accountsByEmployee.Values)
        {
            accounts.TryAdd(bound.Id, bound);
        }

        var headAccountOf = isInvolved ? ActingHeads(snapshot, people, accountsByEmployee) : new Dictionary<Guid, Guid>();
        var tracks = await TracksAsync(cancellationToken);
        var sees = viewer.Sees<CareerTrack>().Compile();
        DeskPrerequisite[] prerequisites = tracks.Any(row => sees(row.Track)) ? [DeskPrerequisite.VisibleTrack] : [];
        var versions = isInvolved ? await VersionsAsync(cancellationToken) : [];
        var lastDecisionOf = await LastDecisionsAsync(versions, cancellationToken);
        var published = await PublishedAsync(versions, viewer, cancellationToken);
        var catalog = viewer.IsAdmin || viewer.IsHr || isInvolved ? await CatalogAsync(viewer, tracks, published, cancellationToken) : CareerDeskCatalog.Empty;
        await AddAccountsAsync(
            [.. versions.Where(version => version.Status != MatrixVersionStatus.Published).Select(version => version.AuthorId), .. catalog.Drafts.Select(draft => draft.AuthorId).OfType<Guid>()],
            accounts,
            people,
            cancellationToken);

        var scan = new CareerDeskScan(viewer, snapshot, assigneeOf, accounts, accountsByEmployee, people, headAccountOf, tracks, versions, lastDecisionOf, time.GetUtcNow());
        return new DeskReport(
            [
                .. scan.UnitFindings(),
                .. scan.VersionFindings(),
                .. scan.PublishedFindings(published, catalog.Directions, catalog.Competencies, catalog.Blocks),
                .. scan.TrackFindings(),
                .. scan.PositionFindings(),
                .. scan.DraftFindings(catalog.Drafts),
                .. viewer.IsHr || viewer.IsAdmin ? CareerDeskScan.UnusedFindings(await store.UnusedEntriesAsync(kind: null, cancellationToken)) : [],
                .. self is null ? [] : await NoTrackFindingsAsync(scan, snapshot, self, versions, isInvolved, cancellationToken),
            ],
            prerequisites);
    }

    /// <summary>
    /// The finding that the user's position has no track, unless a published position matrix for it is in effect in their unit. The matrix
    /// is looked up only when the position has no track, in the versions already read when the user's roles called for them, and otherwise by one query.
    /// </summary>
    private async Task<IEnumerable<DeskFinding>> NoTrackFindingsAsync(
        CareerDeskScan scan,
        OrgSnapshot snapshot,
        EmployeeStatus self,
        IReadOnlyList<CareerDeskVersion> versions,
        bool isInvolved,
        CancellationToken cancellationToken)
    {
        var findings = scan.NoTrackFindings(self).ToList();
        if (findings.Count == 0)
        {
            return findings;
        }

        var ancestry = snapshot.Ancestry(self.OrgUnitId);
        var ancestorIds = ancestry.Select(unit => unit.Id).ToArray();
        HashSet<Guid> matrixUnitIds = isInvolved
            ? [.. versions.Where(version => version.Status == MatrixVersionStatus.Published && version.PositionId == self.PositionId).Select(version => version.UnitId)]
            :
            [
                .. await context.Set<MatrixVersion>()
                    .Where(version => version.Status == MatrixVersionStatus.Published
                        && version.Matrix.PositionId == self.PositionId
                        && ancestorIds.Contains(version.Matrix.OrgUnitId))
                    .Select(version => version.Matrix.OrgUnitId)
                    .ToListAsync(cancellationToken),
            ];
        return MatrixInheritance.Nearest(ancestry, matrixUnitIds.Contains) is null ? findings : [];
    }

    private static IEnumerable<T> Visible<T>(IEnumerable<T> entries, CatalogViewer viewer)
        where T : CatalogEntry => entries.Where(viewer.Sees<T>().Compile());

    private static bool IsDraft(CatalogEntry entry) => entry.Status == CatalogStatus.Draft;

    /// <summary>
    /// Works out, for each unit, whether its head can act, from the accounts of the heads read beforehand.
    /// </summary>
    private static Dictionary<Guid, Guid> ActingHeads(
        OrgSnapshot snapshot,
        IReadOnlyDictionary<Guid, EmployeeStatus> people,
        IReadOnlyDictionary<Guid, UserStatus> accountsByEmployee) => snapshot.Units.Values
        .Select(unit => (unit.Id, AccountId: ProcessRoles.ActingHeadAccount(unit, people, accountsByEmployee)))
        .Where(pair => pair.AccountId is not null)
        .ToDictionary(pair => pair.Id, pair => pair.AccountId!.Value);

    /// <summary>
    /// Reads, in one query, the accounts bound to the assigned HR employees and to the heads the findings need; none when none is needed.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, UserStatus>> AccountsByEmployeeAsync(IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken) =>
        employeeIds.Count == 0
            ? new Dictionary<Guid, UserStatus>()
            : await users.FindByEmployeeIdsAsync([.. employeeIds.Distinct()], cancellationToken);

    /// <summary>
    /// Every track with its levels other than inactive ones and the links of its levels, in one query.
    /// </summary>
    private Task<List<CareerDeskTrack>> TracksAsync(CancellationToken cancellationToken) => context.Set<CareerTrack>()
        .AsNoTracking()
        .Select(track => new CareerDeskTrack(
            track,
            context.Set<CareerLevel>()
                .Where(level => level.TrackId == track.Id && level.Status != CatalogStatus.Inactive)
                .OrderBy(level => level.Position)
                .ToList(),
            context.Set<CareerLevelPosition>().Where(link => link.TrackId == track.Id).ToList()))
        .ToListAsync(cancellationToken);

    private Task<List<CareerDeskVersion>> VersionsAsync(CancellationToken cancellationToken) => context.Set<MatrixVersion>()
        .AsNoTracking()
        .Where(version => version.Status == MatrixVersionStatus.Draft
            || version.Status == MatrixVersionStatus.OnReview
            || version.Status == MatrixVersionStatus.Returned
            || version.Status == MatrixVersionStatus.Published)
        .Select(version => new CareerDeskVersion(
            version.Id,
            version.MatrixId,
            version.Matrix.OrgUnitId,
            version.Matrix.TrackId,
            version.Matrix.PositionId,
            version.Number,
            version.Status,
            version.Step,
            version.AuthorId,
            version.CreatedAt,
            version.BuilderStep))
        .ToListAsync(cancellationToken);

    private async Task<Dictionary<Guid, CareerDeskDecision>> LastDecisionsAsync(IReadOnlyList<CareerDeskVersion> versions, CancellationToken cancellationToken)
    {
        var inWorkIds = versions.Where(version => version.Status != MatrixVersionStatus.Published).Select(version => version.Id).ToList();
        if (inWorkIds.Count == 0)
        {
            return [];
        }

        var decisions = await context.Set<MatrixDecision>()
            .AsNoTracking()
            .Where(decision => inWorkIds.Contains(decision.VersionId))
            .Select(decision => new CareerDeskDecision(decision.VersionId, decision.Sequence, decision.At, decision.Comment != null, decision.VariantNumber != null))
            .ToListAsync(cancellationToken);
        return decisions.GroupBy(decision => decision.VersionId).ToDictionary(group => group.Key, group => group.MaxBy(decision => decision.Sequence)!);
    }

    /// <summary>
    /// The published versions, with their documents, of the matrices the user leads that have no version in work.
    /// </summary>
    private async Task<List<MatrixVersion>> PublishedAsync(IReadOnlyList<CareerDeskVersion> versions, CatalogViewer viewer, CancellationToken cancellationToken)
    {
        var inWorkMatrixIds = versions.Where(version => version.Status != MatrixVersionStatus.Published).Select(version => version.MatrixId).ToHashSet();
        var ids = versions
            .Where(version => version.Status == MatrixVersionStatus.Published
                && viewer.MatrixUnitIds.Contains(version.UnitId)
                && !inWorkMatrixIds.Contains(version.MatrixId))
            .Select(version => version.Id)
            .ToList();
        return ids.Count == 0
            ? []
            : await context.Set<MatrixVersion>().AsNoTracking().Where(version => ids.Contains(version.Id)).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The drafts the user can read, levels aside, with the directions, the competencies the published versions name and the shared blocks
    /// they include, read as they stand now in one query with the draft blocks.
    /// </summary>
    private async Task<CareerDeskCatalog> CatalogAsync(
        CatalogViewer viewer,
        IReadOnlyList<CareerDeskTrack> tracks,
        IReadOnlyList<MatrixVersion> published,
        CancellationToken cancellationToken)
    {
        var competencyIds = published.SelectMany(version => version.Snapshot?.Competencies ?? []).Select(entry => entry.Id).Distinct().ToList();
        var blockIds = published.SelectMany(version => version.Snapshot?.SharedBlocks ?? []).Select(entry => entry.Id).Distinct().ToList();
        var directions = await context.Set<CareerDirection>().AsNoTracking().ToDictionaryAsync(direction => direction.Id, cancellationToken);
        var competencies = await context.Set<CareerCompetency>()
            .AsNoTracking()
            .Where(competency => competency.Status == CatalogStatus.Draft || competencyIds.Contains(competency.Id))
            .ToDictionaryAsync(competency => competency.Id, cancellationToken);
        var scales = await context.Set<CompetencyScale>().AsNoTracking().Where(scale => scale.Status == CatalogStatus.Draft).ToListAsync(cancellationToken);
        var blocks = await context.Set<CompetencyBlock>()
            .AsNoTracking()
            .Where(block => block.Status == CatalogStatus.Draft || blockIds.Contains(block.Id))
            .ToDictionaryAsync(block => block.Id, cancellationToken);
        return new CareerDeskCatalog(
            [
                .. Visible(directions.Values.Where(IsDraft), viewer),
                .. Visible(tracks.Select(row => row.Track).Where(IsDraft), viewer),
                .. Visible(competencies.Values.Where(IsDraft), viewer),
                .. Visible(scales, viewer),
                .. Visible(blocks.Values.Where(IsDraft), viewer),
            ],
            directions,
            competencies,
            blocks);
    }

    /// <summary>
    /// Adds the accounts not yet read, and the employees they are bound to, to those the findings may name.
    /// </summary>
    private async Task AddAccountsAsync(
        IEnumerable<Guid> accountIds,
        Dictionary<Guid, UserStatus> accounts,
        Dictionary<Guid, EmployeeStatus> people,
        CancellationToken cancellationToken)
    {
        var missing = accountIds.Where(id => !accounts.ContainsKey(id)).Distinct().ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var found = await users.FindManyAsync(missing, cancellationToken);
        foreach (var status in found.Values)
        {
            accounts[status.Id] = status;
        }

        await AddEmployeesAsync([.. found.Values.Select(status => status.EmployeeId).OfType<Guid>()], people, cancellationToken);
    }

    /// <summary>
    /// Adds the employees not yet read to those the findings may name.
    /// </summary>
    private async Task AddEmployeesAsync(IReadOnlyCollection<Guid> employeeIds, Dictionary<Guid, EmployeeStatus> people, CancellationToken cancellationToken)
    {
        var missing = employeeIds.Where(id => !people.ContainsKey(id)).Distinct().ToList();
        if (missing.Count == 0)
        {
            return;
        }

        foreach (var employee in (await employees.FindManyAsync(missing, cancellationToken)).Values)
        {
            people[employee.Id] = employee;
        }
    }
}
