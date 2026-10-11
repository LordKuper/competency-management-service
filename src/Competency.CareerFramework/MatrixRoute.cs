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
        foreach (var unit in (await employees.FindAncestryAsync(unitId, cancellationToken)).Skip(1))
        {
            if (await roles.HeadAccountAsync(unit, cancellationToken) is { } accountId)
            {
                return accountId;
            }
        }

        return null;
    }

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
        if (version.Step == ApprovalStep.Superior && await SuperiorApproverAsync(unitId, cancellationToken) is { } superior)
        {
            return new MatrixTurn(ApprovalStep.Superior, superior);
        }

        return new MatrixTurn(ApprovalStep.Hr, await roles.ResponsibleHrAsync(unitId, cancellationToken));
    }

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
    public async Task<bool> CanReadAsync(CatalogViewer viewer, MatrixVersion version, bool isHistory, CancellationToken cancellationToken)
    {
        var unitId = version.Matrix.OrgUnitId;
        if ((version.IsPublic && !isHistory) || viewer.IsAdmin || viewer.HeadedUnitIds.Contains(unitId) || viewer.HrUnitIds.Contains(unitId)
            || version.AuthorId == viewer.UserId)
        {
            return true;
        }

        return await context.Set<MatrixDecision>().AnyAsync(decision => decision.VersionId == version.Id && decision.ActorId == viewer.UserId, cancellationToken)
            || await SuperiorApproverAsync(unitId, cancellationToken) == viewer.UserId;
    }

    /// <summary>
    /// The catalogue entries of the versions in work the user reads as their author, a past participant or the superior approver,
    /// so the drafts among them are readable to the user along with the version.
    /// </summary>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The identifiers of the levels, competencies, categories of those competencies and scales of those versions.</returns>
    public async Task<IReadOnlySet<Guid>> RouteEntryIdsAsync(CatalogViewer viewer, CancellationToken cancellationToken)
    {
        var userId = viewer.UserId;
        var versions = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(IsInWork)
            .Where(version => version.AuthorId == userId
                || context.Set<MatrixDecision>().Any(decision => decision.VersionId == version.Id && decision.ActorId == userId))
            .ToListAsync(cancellationToken);
        versions.AddRange(await SuperiorOfAsync(viewer, cancellationToken));

        var ids = new HashSet<Guid>();
        var competencyIds = new HashSet<Guid>();
        foreach (var version in versions)
        {
            ids.Add(version.ScaleId);
            ids.UnionWith(version.Content.Levels.Select(level => level.LevelId));
            competencyIds.UnionWith(version.Content.Competencies.Select(competency => competency.CompetencyId));
        }

        if (competencyIds.Count > 0)
        {
            var used = competencyIds.ToList();
            ids.UnionWith(used);
            ids.UnionWith(await context.Set<CareerCompetency>()
                .Where(competency => used.Contains(competency.Id))
                .Select(competency => competency.CategoryId)
                .ToListAsync(cancellationToken));
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
        var unitIds = (await UnitsBelowHeadedAsync(viewer, cancellationToken)).Union(viewer.HrUnitIds).ToList();
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

    /// <summary>
    /// The versions in work of the units below the user's headed units for which the user is the superior approver.
    /// </summary>
    private async Task<List<MatrixVersion>> SuperiorOfAsync(CatalogViewer viewer, CancellationToken cancellationToken)
    {
        var belowIds = await UnitsBelowHeadedAsync(viewer, cancellationToken);
        if (belowIds.Count == 0)
        {
            return [];
        }

        var candidates = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(IsInWork)
            .Where(version => belowIds.Contains(version.Matrix.OrgUnitId))
            .ToListAsync(cancellationToken);
        var approverOf = new Dictionary<Guid, Guid?>();
        foreach (var unitId in candidates.Select(version => version.Matrix.OrgUnitId).Distinct())
        {
            approverOf[unitId] = await SuperiorApproverAsync(unitId, cancellationToken);
        }

        return [.. candidates.Where(version => approverOf[version.Matrix.OrgUnitId] == viewer.UserId)];
    }

    private async Task<List<Guid>> UnitsBelowHeadedAsync(CatalogViewer viewer, CancellationToken cancellationToken)
    {
        var belowIds = new HashSet<Guid>();
        foreach (var headedId in viewer.HeadedUnitIds)
        {
            belowIds.UnionWith((await employees.FindSubtreeAsync(headedId, cancellationToken)).Select(unit => unit.Id).Where(id => id != headedId));
        }

        return [.. belowIds];
    }
}
