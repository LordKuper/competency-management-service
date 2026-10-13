using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The one rule of making career entries active. Approving or reactivating a catalogue entry, and publishing a matrix version, make
/// active with the root every draft it refers to, directly or through other entries, or nothing at all: a track takes its direction and
/// draft levels, a competency its category, direction and scale, a version its track with the track's levels, its competencies and,
/// through the active ones too, their drafts, and its scale. Each draft must be one the right that counts may approve, have a name
/// nothing published already has, and as a track be ready. The plan runs under the caller's locks for an action, and without locks
/// for a preview, which then changes nothing because nobody saves it.
/// </summary>
internal sealed class CatalogPublication(AppDbContext context, CatalogStore store, ProcessRoles roles, IAuditWriter audit, ICurrentActor actor)
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
        CompetencyCategory => PublicationEntryKind.Category,
        CareerCompetency => PublicationEntryKind.Competency,
        CompetencyScale => PublicationEntryKind.Scale,
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
            _ => [],
        };
        return await CheckAsync(root.Id, root.Version, root, drafts, approver, cancellationToken);
    }

    /// <summary>
    /// Plans publishing the version by the right of the responsible HR of the matrix unit, whoever starts it; without that HR no draft
    /// may be approved. The drafts are set active on the tracked entries.
    /// </summary>
    /// <param name="version">The version, with its matrix.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The plan; nothing is saved.</returns>
    public async Task<PublicationPlan> PlanAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        var hrId = await roles.ResponsibleHrAsync(version.Matrix.OrgUnitId, cancellationToken);
        var approver = hrId is { } id
            ? await CatalogViewer.LoadAsync(id, id == actor.UserId && actor.Role == PlatformClaims.GlobalAdminRole, roles, cancellationToken)
            : null;

        var track = await context.Set<CareerTrack>().FirstAsync(entry => entry.Id == version.Matrix.TrackId, cancellationToken);
        var competencyIds = version.Content.Competencies.Select(competency => competency.CompetencyId).ToList();
        var competencies = await context.Set<CareerCompetency>()
            .Where(competency => competencyIds.Contains(competency.Id) && competency.Status != CatalogStatus.Inactive)
            .ToListAsync(cancellationToken);
        List<CatalogEntry> drafts =
        [
            .. track.Status == CatalogStatus.Draft ? [track, .. await TrackDraftsAsync(track, cancellationToken)] : await DraftLevelsAsync(track.Id, cancellationToken),
            .. competencies.Where(competency => competency.Status == CatalogStatus.Draft),
            .. await ReferenceDraftsAsync(competencies, cancellationToken),
            .. await DraftsAsync<CompetencyScale>([version.ScaleId], cancellationToken),
        ];
        return await CheckAsync(version.Id, version.Version, root: null, drafts, approver, cancellationToken);
    }

    /// <summary>
    /// Whether a new competency refers to drafts it would publish, so a keeper creates it as a draft rather than active.
    /// </summary>
    /// <param name="competency">The competency about to be created.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>True when its category, direction or scale is a draft.</returns>
    public async Task<bool> HasDraftReferencesAsync(CareerCompetency competency, CancellationToken cancellationToken) =>
        (await ReferenceDraftsAsync([competency], cancellationToken)).Count > 0;

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

    private static (CatalogEntry Entry, PublicationBlocker Blocker) BlockerOf(CatalogEntry entry, PublicationBlockerReason reason) =>
        (entry, new PublicationBlocker(KindOf(entry), entry.Id, entry.Name, reason, ContextOf(entry)));

    /// <summary>
    /// Sets the root and its drafts active and finds what is in the way: drafts the approver may not approve, tracks that are not ready,
    /// and names taken, the root's only when it leaves the draft status. The tag is taken from the versions as read.
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
            [.. entries.Where(entry => approver?.CanManage(entry) != true).Select(entry => BlockerOf(entry, PublicationBlockerReason.NoRight))];
        foreach (var track in published.OfType<CareerTrack>())
        {
            blockers.AddRange((await store.ReadinessAsync(track.Id, cancellationToken)).Blockers(track));
        }

        foreach (var entry in named)
        {
            if (await IsNameTakenAsync(entry, cancellationToken))
            {
                blockers.Add(BlockerOf(entry, PublicationBlockerReason.NameTaken));
            }
        }

        return new PublicationPlan(entries, blockers, TagOf(rootId, rootVersion, entries));
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
        .. await DraftsAsync<CompetencyCategory>([.. competencies.Select(competency => competency.CategoryId)], cancellationToken),
        .. await DraftsAsync<CareerDirection>([.. competencies.Select(competency => competency.DirectionId).OfType<Guid>()], cancellationToken),
        .. await DraftsAsync<CompetencyScale>([.. competencies.Select(competency => competency.ScaleId)], cancellationToken),
    ];

    private async Task<List<T>> DraftsAsync<T>(List<Guid> ids, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        ids.Count == 0
            ? []
            : await context.Set<T>().Where(entry => ids.Contains(entry.Id) && entry.Status == CatalogStatus.Draft).ToListAsync(cancellationToken);

    private Task<bool> IsNameTakenAsync(CatalogEntry entry, CancellationToken cancellationToken) => entry switch
    {
        CareerDirection direction => store.IsNameTakenAsync(direction, CareerDirectionEndpoints.Kind.SameScope?.Invoke(direction), cancellationToken),
        CareerTrack track => store.IsNameTakenAsync(track, CareerTrackEndpoints.Kind.SameScope?.Invoke(track), cancellationToken),
        CareerLevel level => store.IsNameTakenAsync(level, CareerLevelEndpoints.Kind.SameScope?.Invoke(level), cancellationToken),
        CompetencyCategory category => store.IsNameTakenAsync(category, CompetencyCategoryEndpoints.Kind.SameScope?.Invoke(category), cancellationToken),
        CareerCompetency competency => store.IsNameTakenAsync(competency, CompetencyEndpoints.Kind.SameScope?.Invoke(competency), cancellationToken),
        CompetencyScale scale => store.IsNameTakenAsync(scale, CompetencyScaleEndpoints.Kind.SameScope?.Invoke(scale), cancellationToken),
        _ => throw new UnreachableException($"Unknown catalogue entry type {entry.GetType().Name}."),
    };
}
