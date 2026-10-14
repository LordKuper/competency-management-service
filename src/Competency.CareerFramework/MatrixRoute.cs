using System.Linq.Expressions;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The approval route of matrix versions and who may read them, worked out from the org structure and the HR assignments at the
/// moment of asking, so a change of head or HR applies at once. A caller that acts on the answer holds the tree lock first.
/// </summary>
internal sealed class MatrixRoute(AppDbContext context, IEmployeeDirectory employees, ProcessRoles roles)
{
    private static readonly Expression<Func<MatrixVersion, bool>> IsInWork = version =>
        version.Status == MatrixVersionStatus.Draft || version.Status == MatrixVersionStatus.OnReview || version.Status == MatrixVersionStatus.Returned;

    /// <summary>
    /// The approver of the superior step: the head of the nearest unit above the given one whose head can act.
    /// </summary>
    /// <param name="unitId">The matrix unit.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The approver's account, or <see langword="null"/> when no unit above has such a head and the step is skipped.</returns>
    public async Task<Guid?> SuperiorApproverAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var ancestry = await employees.FindAncestryAsync(unitId, cancellationToken);
        return SuperiorApprover(ancestry, await roles.HeadAccountsAsync([.. ancestry.Skip(1)], cancellationToken));
    }

    /// <summary>
    /// The approver of the superior step over data read beforehand, by the rule of <see cref="SuperiorApproverAsync"/>.
    /// </summary>
    /// <param name="ancestry">The matrix unit and the units above it, nearest first.</param>
    /// <param name="headAccountOf">The account of each unit's head who can act, covering at least the units above the matrix unit.</param>
    /// <returns>The approver's account, or <see langword="null"/> when the step is skipped.</returns>
    public static Guid? SuperiorApprover(IReadOnlyList<OrgUnitStatus> ancestry, IReadOnlyDictionary<Guid, Guid> headAccountOf) =>
        ancestry.Skip(1)
            .Select(unit => headAccountOf.TryGetValue(unit.Id, out var accountId) ? accountId : (Guid?)null)
            .FirstOrDefault(accountId => accountId is not null);

    /// <summary>
    /// Whose turn it is on a version on review. A superior step whose approver has gone since is passed over to the HR step.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The turn, or <see langword="null"/> when the version is not on review.</returns>
    public async Task<MatrixTurn?> TurnAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        if (version.Status != MatrixVersionStatus.OnReview)
        {
            return null;
        }

        var unitId = version.Matrix.OrgUnitId;
        var superior = version.Step == ApprovalStep.Superior ? await SuperiorApproverAsync(unitId, cancellationToken) : null;
        return Turn(version.Status, version.Step, superior, superior is null ? await roles.ResponsibleHrAsync(unitId, cancellationToken) : null);
    }

    /// <summary>
    /// Whose turn it is on a version over data read beforehand, by the rule of <see cref="TurnAsync"/>.
    /// </summary>
    /// <param name="status">The version's status.</param>
    /// <param name="step">The step the version waits at.</param>
    /// <param name="superiorApprover">The approver of the superior step; null when the step is skipped.</param>
    /// <param name="responsibleHr">The responsible HR of the matrix unit; needed only when the turn is not the superior approver's.</param>
    /// <returns>The turn, or <see langword="null"/> when the version is not on review.</returns>
    public static MatrixTurn? Turn(MatrixVersionStatus status, ApprovalStep? step, Guid? superiorApprover, Guid? responsibleHr) => status switch
    {
        not MatrixVersionStatus.OnReview => null,
        _ when step == ApprovalStep.Superior && superiorApprover is { } superior => new MatrixTurn(ApprovalStep.Superior, superior),
        _ => new MatrixTurn(ApprovalStep.Hr, responsibleHr),
    };

    /// <summary>
    /// Whether the user may read the version, or its approval history: everyone a published or archived version; otherwise, and for the
    /// history of any version, global administrators, the heads and the responsible HR of the matrix unit, the version's author,
    /// whoever acted on it, and the approver of its superior step.
    /// </summary>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="version">The version.</param>
    /// <param name="isHistory">Whether the user asks for the history rather than the version itself.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>True when the user may read it.</returns>
    public async Task<bool> CanReadAsync(CatalogViewer viewer, MatrixVersion version, bool isHistory, CancellationToken cancellationToken) =>
        ReadsWithoutLookup(viewer, version, isHistory)
        || await context.Set<MatrixDecision>().AnyAsync(decision => decision.VersionId == version.Id && decision.ActorId == viewer.UserId, cancellationToken)
        || await IsSuperiorApproverAsync(viewer, version.Matrix.OrgUnitId, cancellationToken);

    /// <summary>
    /// The versions the user may read, by the rule of <see cref="CanReadAsync"/>, for a list: whoever acted on the versions is found in
    /// one query, and the superior approver once per unit.
    /// </summary>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="versions">The versions.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The readable versions in their given order.</returns>
    public async Task<List<MatrixVersion>> ReadableAsync(CatalogViewer viewer, IReadOnlyList<MatrixVersion> versions, CancellationToken cancellationToken)
    {
        var userId = viewer.UserId;
        var pendingIds = versions.Where(version => !ReadsWithoutLookup(viewer, version, isHistory: false)).Select(version => version.Id).ToList();
        var actedOnIds = pendingIds.Count == 0
            ? []
            : (await context.Set<MatrixDecision>()
                .Where(decision => pendingIds.Contains(decision.VersionId) && decision.ActorId == userId)
                .Select(decision => decision.VersionId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();
        var superiorOf = new Dictionary<Guid, bool>();
        var readable = new List<MatrixVersion>();
        foreach (var version in versions)
        {
            if (ReadsWithoutLookup(viewer, version, isHistory: false) || actedOnIds.Contains(version.Id))
            {
                readable.Add(version);
                continue;
            }

            var unitId = version.Matrix.OrgUnitId;
            if (!superiorOf.TryGetValue(unitId, out var isSuperior))
            {
                isSuperior = await IsSuperiorApproverAsync(viewer, unitId, cancellationToken);
                superiorOf[unitId] = isSuperior;
            }

            if (isSuperior)
            {
                readable.Add(version);
            }
        }

        return readable;
    }

    /// <summary>
    /// The catalogue entries of the versions in work the user reads as their author, a past participant or the superior approver,
    /// so the drafts among them are readable to the user along with the version.
    /// </summary>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="below">The units strictly below the user's headed units, as <see cref="UnitsBelowHeadedAsync"/> finds them.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The identifiers of the levels, shared blocks, competencies with those of the blocks, categories of those competencies and
    /// scales of those competencies and blocks.</returns>
    public async Task<IReadOnlySet<Guid>> RouteEntryIdsAsync(
        CatalogViewer viewer,
        IReadOnlyDictionary<Guid, OrgUnitStatus> below,
        CancellationToken cancellationToken)
    {
        var userId = viewer.UserId;
        var versions = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(IsInWork)
            .Where(version => version.AuthorId == userId
                || context.Set<MatrixDecision>().Any(decision => decision.VersionId == version.Id && decision.ActorId == userId))
            .ToListAsync(cancellationToken);
        versions.AddRange(await SuperiorOfAsync(viewer, below, cancellationToken));

        var ids = new HashSet<Guid>();
        var competencyIds = new HashSet<Guid>();
        foreach (var version in versions)
        {
            ids.UnionWith(version.Content.Levels.Select(level => level.LevelId));
            ids.UnionWith(version.Content.SharedBlocks.Select(block => block.BlockId));
            competencyIds.UnionWith(version.Content.Competencies.Select(competency => competency.CompetencyId));
        }

        var blockIds = versions.SelectMany(version => version.Content.SharedBlocks).Select(block => block.BlockId).Distinct().ToList();
        if (blockIds.Count > 0)
        {
            foreach (var block in await context.Set<CompetencyBlock>().AsNoTracking().Where(block => blockIds.Contains(block.Id)).ToListAsync(cancellationToken))
            {
                ids.Add(block.ScaleId);
                competencyIds.UnionWith(block.CompetencyIds);
            }
        }

        if (competencyIds.Count > 0)
        {
            var used = competencyIds.ToList();
            ids.UnionWith(used);
            foreach (var competency in await context.Set<CareerCompetency>()
                         .Where(competency => used.Contains(competency.Id))
                         .Select(competency => new { competency.CategoryId, competency.ScaleId })
                         .ToListAsync(cancellationToken))
            {
                ids.Add(competency.CategoryId);
                ids.Add(competency.ScaleId);
            }
        }

        return ids;
    }

    /// <summary>
    /// The versions on review whose current approver is the user. Candidates are the versions of the units below the user's headed
    /// units and of the units the user is the responsible HR of.
    /// </summary>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>Each version with its turn.</returns>
    public async Task<IReadOnlyList<(MatrixVersion Version, MatrixTurn Turn)>> AwaitingAsync(CatalogViewer viewer, CancellationToken cancellationToken)
    {
        var unitIds = (await UnitsBelowHeadedAsync(viewer, cancellationToken)).Keys.Union(viewer.HrUnitIds).ToList();
        var versions = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(version => version.Status == MatrixVersionStatus.OnReview && unitIds.Contains(version.Matrix.OrgUnitId))
            .ToListAsync(cancellationToken);

        var awaiting = new List<(MatrixVersion, MatrixTurn)>();
        foreach (var version in versions)
        {
            if (await TurnAsync(version, cancellationToken) is { } turn && turn.ApproverId == viewer.UserId)
            {
                awaiting.Add((version, turn));
            }
        }

        return awaiting;
    }

    private static bool ReadsWithoutLookup(CatalogViewer viewer, MatrixVersion version, bool isHistory) =>
        (version.IsPublic && !isHistory) || ReadsAnyVersion(viewer, version.Matrix.OrgUnitId, version.AuthorId);

    /// <summary>
    /// Whether the user reads a version of the unit's matrix whatever its status, without looking up who acted on it: global administrators,
    /// the heads and the responsible HR of the unit, and the version's author. The superior approver and past participants read it too, by a lookup.
    /// </summary>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="unitId">The matrix unit.</param>
    /// <param name="authorId">The version's author.</param>
    /// <returns>True when the user reads it.</returns>
    public static bool ReadsAnyVersion(CatalogViewer viewer, Guid unitId, Guid authorId) =>
        viewer.IsAdmin || viewer.HeadedUnitIds.Contains(unitId) || viewer.HrUnitIds.Contains(unitId) || authorId == viewer.UserId;

    /// <summary>
    /// Whether the user is the superior approver of the unit; the approver heads the unit above, so a user who heads none is not.
    /// </summary>
    private async Task<bool> IsSuperiorApproverAsync(CatalogViewer viewer, Guid unitId, CancellationToken cancellationToken) =>
        viewer.HeadedUnitIds.Count > 0 && await SuperiorApproverAsync(unitId, cancellationToken) == viewer.UserId;

    /// <summary>
    /// The versions in work of the units below the user's headed units for which the user is the superior approver: walking up from a
    /// unit's parent, the first unit whose head can act is one the user heads. The walk stays inside the headed subtrees, so no ancestry
    /// is read, and the head of each unit on the way is looked up once.
    /// </summary>
    private async Task<List<MatrixVersion>> SuperiorOfAsync(
        CatalogViewer viewer,
        IReadOnlyDictionary<Guid, OrgUnitStatus> below,
        CancellationToken cancellationToken)
    {
        if (below.Count == 0)
        {
            return [];
        }

        var belowIds = below.Keys.ToList();
        var candidates = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(IsInWork)
            .Where(version => belowIds.Contains(version.Matrix.OrgUnitId))
            .ToListAsync(cancellationToken);
        var headOf = new Dictionary<Guid, Guid?>();
        var approvedUnitIds = new HashSet<Guid>();
        foreach (var unitId in candidates.Select(version => version.Matrix.OrgUnitId).Distinct())
        {
            if (await NearestActingHeadIsViewerAsync(below[unitId].ParentId, viewer, below, headOf, cancellationToken))
            {
                approvedUnitIds.Add(unitId);
            }
        }

        return [.. candidates.Where(version => approvedUnitIds.Contains(version.Matrix.OrgUnitId))];
    }

    private async Task<bool> NearestActingHeadIsViewerAsync(
        Guid? startId,
        CatalogViewer viewer,
        IReadOnlyDictionary<Guid, OrgUnitStatus> below,
        Dictionary<Guid, Guid?> headOf,
        CancellationToken cancellationToken)
    {
        var current = startId;
        while (current is { } id && !viewer.HeadedUnitIds.Contains(id) && below.TryGetValue(id, out var unit))
        {
            if (!headOf.TryGetValue(id, out var head))
            {
                head = await roles.HeadAccountAsync(unit, cancellationToken);
                headOf[id] = head;
            }

            if (head is not null)
            {
                return false;
            }

            current = unit.ParentId;
        }

        return current is { } reached && viewer.HeadedUnitIds.Contains(reached);
    }

    /// <summary>
    /// The units strictly below the user's headed units, by identifier.
    /// </summary>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The units by identifier.</returns>
    public async Task<Dictionary<Guid, OrgUnitStatus>> UnitsBelowHeadedAsync(CatalogViewer viewer, CancellationToken cancellationToken)
    {
        var below = new Dictionary<Guid, OrgUnitStatus>();
        foreach (var headedId in viewer.HeadedUnitIds)
        {
            foreach (var unit in (await employees.FindSubtreeAsync(headedId, cancellationToken)).Where(unit => unit.Id != headedId))
            {
                below.TryAdd(unit.Id, unit);
            }
        }

        return below;
    }
}
