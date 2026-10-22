using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The one rule of making career entries active. Approving or reactivating a catalogue entry, and publishing a matrix version, make
/// active with the root every draft it refers to, directly or through other entries, or nothing at all: a track takes its direction and
/// draft levels, a competency its direction and scale, a shared block the competencies of its composition, a
/// version the track of a track matrix with the track's levels, its shared blocks, its competencies and, through the active ones too,
/// their drafts. Each draft must be one the right that counts may approve with it, have a name nothing published already has, as
/// a track be ready, and as a shared block have competencies. The plan runs under the caller's locks for an action, and without locks
/// for a preview, which then changes nothing because nobody saves it.
/// </summary>
internal sealed class CatalogPublication(AppDbContext context, CatalogStore store, ProcessRoles roles, IAuditWriter audit, IUserDirectory users)
{
    private const string ApprovedAction = "Approved";

    /// <summary>
    /// The kind a list shows for the entry.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns>The kind.</returns>
    public static PublicationEntryKind KindOf(CatalogEntry entry) => entry switch
    {
        CareerDirection => PublicationEntryKind.Direction,
        CareerTrack => PublicationEntryKind.Track,
        CareerLevel => PublicationEntryKind.Level,
        CareerCompetency => PublicationEntryKind.Competency,
        CompetencyScale => PublicationEntryKind.Scale,
        CompetencyBlock => PublicationEntryKind.Block,
        _ => throw new UnreachableException($"Unknown catalogue entry type {entry.GetType().Name}."),
    };

    /// <summary>
    /// Where a list says the entry sits: a level's track name.
    /// </summary>
    /// <param name="entry">The entry; a level with its track loaded.</param>
    /// <returns>The context, or null for the kinds that need none.</returns>
    public static string? ContextOf(CatalogEntry entry) => entry is CareerLevel level ? level.Track.Name : null;

    /// <summary>
    /// Plans making a draft or inactive entry active by the right of the given user; an entry that is already active publishes nothing.
    /// The root and its drafts are set active on the tracked entries, so names are checked against everything published together.
    /// </summary>
    /// <param name="root">The tracked entry; the caller has checked that the user may manage it.</param>
    /// <param name="approver">The rights of the user who publishes.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The plan; nothing is saved.</returns>
    public async Task<PublicationPlan> PlanAsync(CatalogEntry root, CatalogViewer approver, CancellationToken cancellationToken)
    {
        if (root.Status == CatalogStatus.Active)
        {
            return new PublicationPlan([], [], TagOf(root.Id, root.Version, []));
        }

        List<CatalogEntry> drafts = root switch
        {
            CareerTrack track => await TrackDraftsAsync(track, cancellationToken),
            CareerCompetency competency => await ReferenceDraftsAsync([competency], cancellationToken),
            CompetencyBlock block => await BlockDraftsAsync([block], cancellationToken),
            _ => [],
        };
        return await CheckAsync(root.Id, root.Version, root, drafts, approver, cancellationToken);
    }

    /// <summary>
    /// Plans publishing the version by the right of the responsible HR of the matrix unit, with that HR's own roles, whoever starts it;
    /// without that HR no draft may be approved. A responsible HR always holds the HR role, since an assignment acts only through such an account.
    /// The drafts are set active on the tracked entries.
    /// </summary>
    /// <param name="version">The version, with its matrix.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The plan; nothing is saved.</returns>
    public async Task<PublicationPlan> PlanAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        var hr = await roles.ResponsibleHrAsync(version.Matrix.OrgUnitId, cancellationToken) is { } hrId
            ? await users.FindAsync(hrId, cancellationToken)
            : null;
        var approver = hr is null
            ? null
            : await CatalogViewer.LoadAsync(hr.Id, hr.IsGlobalAdmin, isHr: true, roles, cancellationToken);

        var blocks = await NotInactiveAsync<CompetencyBlock>([.. version.Content.SharedBlocks.Select(block => block.BlockId)], cancellationToken);
        var competencies = await NotInactiveAsync<CareerCompetency>([.. version.Content.Competencies.Select(competency => competency.CompetencyId)], cancellationToken);
        List<CatalogEntry> drafts =
        [
            .. await MatrixTrackDraftsAsync(version.Matrix, cancellationToken),
            .. blocks.Where(IsDraft),
            .. await BlockDraftsAsync(blocks, cancellationToken),
            .. competencies.Where(IsDraft),
            .. await ReferenceDraftsAsync(competencies, cancellationToken),
        ];
        return await CheckAsync(version.Id, version.Version, root: null, drafts, approver, cancellationToken);
    }

    /// <summary>
    /// Whether a new competency or shared block refers to drafts it would publish, so a keeper creates it as a draft rather than active.
    /// </summary>
    /// <param name="entry">The competency or shared block about to be created.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>True when an entry it refers to, directly or through the competencies of a block, is a draft.</returns>
    public async Task<bool> HasDraftReferencesAsync(CatalogEntry entry, CancellationToken cancellationToken)
    {
        List<CatalogEntry> drafts = entry switch
        {
            CareerCompetency competency => await ReferenceDraftsAsync([competency], cancellationToken),
            CompetencyBlock block => await BlockDraftsAsync([block], cancellationToken),
            _ => throw new UnreachableException($"Entries of type {entry.GetType().Name} refer to no drafts on creation."),
        };
        return drafts.Count > 0;
    }

    /// <summary>
    /// Journals each draft of the plan as approved together with the root; the caller saves them with its own change.
    /// </summary>
    /// <param name="plan">A plan with no blockers.</param>
    /// <param name="rootType">The root's entity type, such as <c>MatrixVersion</c>.</param>
    /// <param name="rootId">The root.</param>
    public void Apply(PublicationPlan plan, string rootType, Guid rootId)
    {
        var reason = $"каскад {rootType} {rootId}";
        foreach (var entry in plan.Entries)
        {
            StageApproved(entry, reason);
        }
    }

    /// <summary>
    /// Journals the approval of an entry as an event of its own, staged for the next save.
    /// </summary>
    /// <param name="entry">The approved entry.</param>
    /// <param name="reason">The root the entry was published with; null for the root itself.</param>
    public void StageApproved(CatalogEntry entry, string? reason = null)
    {
        var entityType = entry.GetType().Name;
        audit.Stage(new AuditEntry($"{entityType}.{ApprovedAction}", entityType, entry.Id.ToString(), Reason: reason), context);
    }

    private static string TagOf(Guid rootId, int rootVersion, IEnumerable<CatalogEntry> entries)
    {
        var text = string.Join(';', [$"{rootId}:{rootVersion}", .. entries.OrderBy(entry => entry.Id).Select(entry => $"{entry.Id}:{entry.Version}")]);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static bool IsDraft(CatalogEntry entry) => entry.Status == CatalogStatus.Draft;

    private static (CatalogEntry Entry, PublicationBlocker Blocker) BlockerOf(CatalogEntry entry, PublicationBlockerReason reason) =>
        (entry, new PublicationBlocker(KindOf(entry), entry.Id, entry.Name, reason, ContextOf(entry)));

    /// <summary>
    /// Sets the root and its drafts active and finds what is in the way: drafts the approver may not approve, tracks that are not ready,
    /// shared blocks without competencies, and names taken, the root's only when it leaves the draft status. The tag is taken from the versions as read.
    /// </summary>
    private async Task<PublicationPlan> CheckAsync(
        Guid rootId,
        int rootVersion,
        CatalogEntry? root,
        List<CatalogEntry> drafts,
        CatalogViewer? approver,
        CancellationToken cancellationToken)
    {
        List<CatalogEntry> entries = [.. drafts
            .DistinctBy(entry => entry.Id)
            .OrderBy(KindOf)
            .ThenBy(entry => (entry as CareerLevel)?.Position)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)];
        List<CatalogEntry> named = [.. root is { Status: CatalogStatus.Draft } ? [root] : Array.Empty<CatalogEntry>(), .. entries];
        List<CatalogEntry> published = [.. root is null ? Array.Empty<CatalogEntry>() : [root], .. entries];
        foreach (var entry in published)
        {
            entry.Status = CatalogStatus.Active;
        }

        List<(CatalogEntry Entry, PublicationBlocker Blocker)> blockers =
            [.. entries.Where(entry => approver?.MayPublishWith(entry) != true).Select(entry => BlockerOf(entry, PublicationBlockerReason.NoRight))];
        foreach (var track in published.OfType<CareerTrack>())
        {
            blockers.AddRange((await store.ReadinessAsync(track.Id, cancellationToken)).Blockers(track));
        }

        blockers.AddRange(published.OfType<CompetencyBlock>()
            .Where(block => block.CompetencyIds.Count == 0)
            .Select(block => BlockerOf(block, PublicationBlockerReason.EmptyBlock)));

        foreach (var entry in named)
        {
            if (await IsNameTakenAsync(entry, cancellationToken))
            {
                blockers.Add(BlockerOf(entry, PublicationBlockerReason.NameTaken));
            }
        }

        return new PublicationPlan(entries, blockers, TagOf(rootId, rootVersion, entries));
    }

    /// <summary>
    /// The drafts of the matrix track: the track with its direction and draft levels when it is a draft, its draft levels otherwise; none for a position matrix.
    /// </summary>
    private async Task<List<CatalogEntry>> MatrixTrackDraftsAsync(CompetencyMatrix matrix, CancellationToken cancellationToken)
    {
        if (matrix.TrackId is not { } trackId)
        {
            return [];
        }

        var track = await context.Set<CareerTrack>().FirstAsync(entry => entry.Id == trackId, cancellationToken);
        return track.Status == CatalogStatus.Draft ? [track, .. await TrackDraftsAsync(track, cancellationToken)] : await DraftLevelsAsync(trackId, cancellationToken);
    }

    /// <summary>
    /// The drafts of shared blocks: the draft competencies of their compositions, and through those competencies other than inactive ones
    /// their own drafts, scales among them.
    /// </summary>
    private async Task<List<CatalogEntry>> BlockDraftsAsync(IReadOnlyCollection<CompetencyBlock> blocks, CancellationToken cancellationToken)
    {
        var competencies = await NotInactiveAsync<CareerCompetency>([.. blocks.SelectMany(block => block.CompetencyIds).Distinct()], cancellationToken);
        return
        [
            .. competencies.Where(IsDraft),
            .. await ReferenceDraftsAsync(competencies, cancellationToken),
        ];
    }

    private async Task<List<CatalogEntry>> TrackDraftsAsync(CareerTrack track, CancellationToken cancellationToken) =>
    [
        .. await DraftsAsync<CareerDirection>([track.DirectionId], cancellationToken),
        .. await DraftLevelsAsync(track.Id, cancellationToken),
    ];

    private async Task<List<CatalogEntry>> DraftLevelsAsync(Guid trackId, CancellationToken cancellationToken) =>
    [
        .. await context.Set<CareerLevel>()
            .Where(level => level.TrackId == trackId && level.Status == CatalogStatus.Draft)
            .ToListAsync(cancellationToken),
    ];

    private async Task<List<CatalogEntry>> ReferenceDraftsAsync(IReadOnlyCollection<CareerCompetency> competencies, CancellationToken cancellationToken) =>
    [
        .. await DraftsAsync<CareerDirection>([.. competencies.Select(competency => competency.DirectionId).OfType<Guid>()], cancellationToken),
        .. await DraftsAsync<CompetencyScale>([.. competencies.Select(competency => competency.ScaleId)], cancellationToken),
    ];

    private async Task<List<T>> DraftsAsync<T>(List<Guid> ids, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        ids.Count == 0
            ? []
            : await context.Set<T>().Where(entry => ids.Contains(entry.Id) && entry.Status == CatalogStatus.Draft).ToListAsync(cancellationToken);

    private async Task<List<T>> NotInactiveAsync<T>(List<Guid> ids, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        ids.Count == 0
            ? []
            : await context.Set<T>().Where(entry => ids.Contains(entry.Id) && entry.Status != CatalogStatus.Inactive).ToListAsync(cancellationToken);

    private Task<bool> IsNameTakenAsync(CatalogEntry entry, CancellationToken cancellationToken) => entry switch
    {
        CareerDirection direction => store.IsNameTakenAsync(direction, CareerDirectionEndpoints.Kind.SameScope?.Invoke(direction), cancellationToken),
        CareerTrack track => store.IsNameTakenAsync(track, CareerTrackEndpoints.Kind.SameScope?.Invoke(track), cancellationToken),
        CareerLevel level => store.IsNameTakenAsync(level, CareerLevelEndpoints.Kind.SameScope?.Invoke(level), cancellationToken),
        CareerCompetency competency => store.IsNameTakenAsync(competency, CompetencyEndpoints.Kind.SameScope?.Invoke(competency), cancellationToken),
        CompetencyScale scale => store.IsNameTakenAsync(scale, CompetencyScaleEndpoints.Kind.SameScope?.Invoke(scale), cancellationToken),
        CompetencyBlock block => store.IsNameTakenAsync(block, CompetencyBlockEndpoints.Kind.SameScope?.Invoke(block), cancellationToken),
        _ => throw new UnreachableException($"Unknown catalogue entry type {entry.GetType().Name}."),
    };
}
