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
    public async Task<IReadOnlyList<DeskFinding>> FindAsync(CancellationToken cancellationToken)
    {
        if (actor.UserId is not { } userId)
        {
            return [];
        }

        var assigneeOf = await roles.AllAssigneesAsync(cancellationToken);
        var accounts = new Dictionary<Guid, UserStatus>(await users.FindManyAsync([userId, .. assigneeOf.Values.Distinct()], cancellationToken));
        if (accounts.GetValueOrDefault(userId) is not { IsActive: true } account)
        {
            return [];
        }

        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        var hrUnitIds = ProcessRoles.HrUnits(snapshot, assigneeOf, account).Select(unit => unit.Id).ToHashSet();
        var mayHead = account.EmployeeId is { } employeeId && snapshot.Units.Values.Any(unit => unit.HeadEmployeeId == employeeId);
        List<Guid> headIds = mayHead || hrUnitIds.Count > 0
            ? [.. snapshot.Units.Values.Where(unit => unit.IsActive).Select(unit => unit.HeadEmployeeId).OfType<Guid>().Distinct()]
            : [];
        var people = new Dictionary<Guid, EmployeeStatus>();
        await AddEmployeesAsync([.. headIds, .. accounts.Values.Select(status => status.EmployeeId).OfType<Guid>()], people, cancellationToken);
        var self = account.EmployeeId is { } selfId ? people.GetValueOrDefault(selfId) : null;
        var viewer = new CatalogViewer(
            userId,
            actor.Role == PlatformClaims.GlobalAdminRole,
            ProcessRoles.HeadedUnits(snapshot.Units.Values, account, self).Select(unit => unit.Id).ToHashSet(),
            hrUnitIds);
        var isInvolved = viewer.HeadedUnitIds.Count > 0 || viewer.IsHr;

        var headAccountOf = isInvolved ? await ActingHeadsAsync(snapshot, headIds, people, accounts, cancellationToken) : new Dictionary<Guid, Guid>();
        var tracks = await TracksAsync(cancellationToken);
        var versions = isInvolved ? await VersionsAsync(cancellationToken) : [];
        var lastDecisionOf = await LastDecisionsAsync(versions, cancellationToken);
        var published = await PublishedAsync(versions, viewer, cancellationToken);
        var catalog = viewer.IsAdmin || isInvolved ? await CatalogAsync(viewer, tracks, published, cancellationToken) : CareerDeskCatalog.Empty;
        await AddAccountsAsync(
            [.. versions.Where(version => version.Status != MatrixVersionStatus.Published).Select(version => version.AuthorId), .. catalog.Drafts.Select(draft => draft.AuthorId).OfType<Guid>()],
            accounts,
            people,
            cancellationToken);

        var scan = new CareerDeskScan(viewer, snapshot, assigneeOf, accounts, people, headAccountOf, tracks, versions, lastDecisionOf, time.GetUtcNow());
        return
        [
            .. scan.UnitFindings(),
            .. scan.VersionFindings(),
            .. scan.PublishedFindings(published, catalog.Directions, catalog.Competencies, catalog.Blocks),
            .. scan.TrackFindings(),
            .. scan.PositionFindings(),
            .. scan.DraftFindings(catalog.Drafts),
            .. viewer.IsHr || viewer.IsAdmin ? CareerDeskScan.UnusedFindings(await store.UnusedEntriesAsync(cancellationToken)) : [],
            .. self is null ? [] : await NoTrackFindingsAsync(scan, self, versions, isInvolved, cancellationToken),
        ];
    }

    /// <summary>
    /// The finding that the user's position has no track, unless their unit has a published position matrix for it. The matrix is looked
    /// up only when the position has no track, in the versions already read when the user's roles called for them, and otherwise by one query.
    /// </summary>
    private async Task<IEnumerable<DeskFinding>> NoTrackFindingsAsync(
        CareerDeskScan scan,
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

        var hasPositionMatrix = isInvolved
            ? versions.Any(version => version.Status == MatrixVersionStatus.Published && version.UnitId == self.OrgUnitId && version.PositionId == self.PositionId)
            : await context.Set<MatrixVersion>().AnyAsync(
                version => version.Status == MatrixVersionStatus.Published
                    && version.Matrix.OrgUnitId == self.OrgUnitId
                    && version.Matrix.PositionId == self.PositionId,
                cancellationToken);
        return hasPositionMatrix ? [] : findings;
    }

    private static IEnumerable<T> Visible<T>(IEnumerable<T> entries, CatalogViewer viewer)
        where T : CatalogEntry => entries.Where(viewer.Sees<T>().Compile());

    private static bool IsDraft(CatalogEntry entry) => entry.Status == CatalogStatus.Draft;

    /// <summary>
    /// Finds the accounts of the heads of the active units and works out, for each unit, whether its head can act; the accounts join those the findings may name.
    /// </summary>
    private async Task<Dictionary<Guid, Guid>> ActingHeadsAsync(
        OrgSnapshot snapshot,
        List<Guid> headIds,
        IReadOnlyDictionary<Guid, EmployeeStatus> people,
        Dictionary<Guid, UserStatus> accounts,
        CancellationToken cancellationToken)
    {
        var accountsByEmployee = await users.FindByEmployeeIdsAsync(headIds, cancellationToken);
        foreach (var headAccount in accountsByEmployee.Values)
        {
            accounts.TryAdd(headAccount.Id, headAccount);
        }

        return snapshot.Units.Values
            .Select(unit => (unit.Id, AccountId: ProcessRoles.ActingHeadAccount(unit, people, accountsByEmployee)))
            .Where(pair => pair.AccountId is not null)
            .ToDictionary(pair => pair.Id, pair => pair.AccountId!.Value);
    }

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
    /// The published versions, with their documents, of the matrices of the units the user heads that have no version in work.
    /// </summary>
    private async Task<List<MatrixVersion>> PublishedAsync(IReadOnlyList<CareerDeskVersion> versions, CatalogViewer viewer, CancellationToken cancellationToken)
    {
        var inWorkMatrixIds = versions.Where(version => version.Status != MatrixVersionStatus.Published).Select(version => version.MatrixId).ToHashSet();
        var ids = versions
            .Where(version => version.Status == MatrixVersionStatus.Published
                && viewer.HeadedUnitIds.Contains(version.UnitId)
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
