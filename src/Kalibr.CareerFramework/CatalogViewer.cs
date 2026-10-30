using System.Diagnostics;
using System.Linq.Expressions;
using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// What the signed-in user may see and do in the career catalogues, from their account roles and their process roles at the moment of asking.
/// An HR is an account with the HR role, whatever its assignments; a responsible HR of a unit is the one its assignment names.
/// Global administrators and every HR keep the entries no unit keeps, and a unit head proposes drafts of them. A track is kept by global
/// administrators and the responsible HR of its unit, and its unit's head edits it while it is a draft; a level goes with its track.
/// Every HR sees active and inactive entries. A draft of no unit is seen by its author, every HR and global administrators; a draft track or
/// level by its author, the head and the responsible HR of its unit and global administrators; a draft shared block that applies to a unit
/// also by that unit's head. Every HR keeps every shared block, and a unit head proposes and edits drafts of blocks that apply to a unit of
/// their subtrees. Whoever reads a matrix version in work also reads the drafts it uses. Everyone sees active entries.
/// </summary>
/// <param name="UserId">The signed-in account.</param>
/// <param name="IsAdmin">Whether the account is a global administrator.</param>
/// <param name="IsHr">Whether the account has the HR role, which keeps the entries of no unit; an assignment neither gives nor needs it.</param>
/// <param name="HeadedUnitIds">The units the account heads.</param>
/// <param name="MatrixUnitIds">The units whose matrices the account leads: the units it heads and the units below them without a head, down to a unit with one.</param>
/// <param name="HrUnitIds">The units the account is the responsible HR of.</param>
internal sealed record CatalogViewer(Guid UserId, bool IsAdmin, bool IsHr, IReadOnlySet<Guid> HeadedUnitIds, IReadOnlySet<Guid> MatrixUnitIds, IReadOnlySet<Guid> HrUnitIds)
{
    /// <summary>
    /// The entries of the matrix versions in work the user reads through the approval route rather than as a keeper or head of their unit.
    /// </summary>
    public IReadOnlySet<Guid> RouteEntryIds { get; init; } = new HashSet<Guid>();

    /// <summary>
    /// The units the user heads and every unit below them, within which a unit head proposes and edits shared blocks. Only
    /// <see cref="CatalogStore.ViewerAsync"/> works it out; a viewer loaded otherwise has none, so it grants a head no right over blocks.
    /// </summary>
    public IReadOnlySet<Guid> HeadedSubtreeIds { get; init; } = new HashSet<Guid>();

    /// <summary>
    /// Works out the signed-in user's rights; a writer calls it under the catalogue lock, so the rights hold until it commits.
    /// </summary>
    /// <param name="actor">The signed-in user.</param>
    /// <param name="roles">The process roles service.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The user's rights.</returns>
    public static Task<CatalogViewer> LoadAsync(ICurrentActor actor, ProcessRoles roles, CancellationToken cancellationToken) =>
        LoadAsync(actor.UserId ?? Guid.Empty, actor.IsGlobalAdmin(), actor.IsHr(), roles, cancellationToken);

    /// <summary>
    /// Works out the rights of an account other than the signed-in one, such as the responsible HR whose right publishes a matrix version.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="isAdmin">Whether the account is a global administrator.</param>
    /// <param name="isHr">Whether the account has the HR role.</param>
    /// <param name="roles">The process roles service.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The account's rights.</returns>
    public static async Task<CatalogViewer> LoadAsync(Guid userId, bool isAdmin, bool isHr, ProcessRoles roles, CancellationToken cancellationToken)
    {
        var headedUnits = await roles.HeadedUnitsAsync(userId, cancellationToken);
        var matrixUnits = await roles.MatrixUnitsAsync(headedUnits, cancellationToken);
        var hrUnits = await roles.HrUnitsAsync(userId, cancellationToken);
        return new CatalogViewer(
            userId,
            isAdmin,
            isHr,
            headedUnits.Select(unit => unit.Id).ToHashSet(),
            matrixUnits.Select(unit => unit.Id).ToHashSet(),
            hrUnits.Select(unit => unit.Id).ToHashSet());
    }

    /// <summary>
    /// The filter of the entries the user may read, for use in database queries; the single place that decides read access.
    /// </summary>
    /// <typeparam name="T">The entry type.</typeparam>
    /// <returns>The filter.</returns>
    public Expression<Func<T, bool>> Sees<T>()
        where T : CatalogEntry
    {
        var isAdmin = IsAdmin;
        var seesInactive = IsAdmin || IsHr;
        var userId = UserId;
        var unitIds = HeadedUnitIds.Union(HrUnitIds).ToList();
        var routeEntryIds = RouteEntryIds.ToList();
        var headedUnitIds = HeadedUnitIds.ToList();
        LambdaExpression sees = typeof(T) == typeof(CompetencyBlock)
            ? (Expression<Func<CompetencyBlock, bool>>)(block => block.Status == CatalogStatus.Active
                || (block.Status == CatalogStatus.Inactive && seesInactive)
                || (block.Status == CatalogStatus.Draft && (seesInactive || block.AuthorId == userId || routeEntryIds.Contains(block.Id)
                    || (block.ScopeUnitId != null && headedUnitIds.Contains(block.ScopeUnitId.Value)))))
            : typeof(T) == typeof(CareerTrack)
            ? (Expression<Func<CareerTrack, bool>>)(track => track.Status == CatalogStatus.Active
                || (track.Status == CatalogStatus.Inactive && seesInactive)
                || (track.Status == CatalogStatus.Draft
                    && (isAdmin || track.AuthorId == userId || unitIds.Contains(track.OwnerOrgUnitId) || routeEntryIds.Contains(track.Id))))
            : typeof(T) == typeof(CareerLevel)
                ? (Expression<Func<CareerLevel, bool>>)(level => level.Status == CatalogStatus.Active
                    || (level.Status == CatalogStatus.Inactive && seesInactive)
                    || (level.Status == CatalogStatus.Draft
                        && (isAdmin || level.AuthorId == userId || unitIds.Contains(level.Track.OwnerOrgUnitId) || routeEntryIds.Contains(level.Id))))
                : (Expression<Func<T, bool>>)(entry => entry.Status == CatalogStatus.Active
                    || (entry.Status == CatalogStatus.Inactive && seesInactive)
                    || (entry.Status == CatalogStatus.Draft && (seesInactive || entry.AuthorId == userId || routeEntryIds.Contains(entry.Id))));
        return (Expression<Func<T, bool>>)sees;
    }

    /// <summary>
    /// The check whether the user may read entries already loaded, by the same filter as <see cref="Sees{T}"/>. Compiling a filter costs
    /// far more than applying it, so the check compiles each entry type's filter once, on its first entry; take one per list of entries.
    /// </summary>
    /// <returns>The check, given an entry and, for a level, its track loaded.</returns>
    public Func<CatalogEntry, bool> SeesLoaded()
    {
        var compiled = new Dictionary<Type, Func<CatalogEntry, bool>>();
        return entry =>
        {
            if (!compiled.TryGetValue(entry.GetType(), out var sees))
            {
                sees = Compiled(entry);
                compiled[entry.GetType()] = sees;
            }

            return sees(entry);
        };
    }

    /// <summary>
    /// Whether the user may change the entry's fields: its keepers, and while it is a draft the head of its unit, or for an entry of no unit
    /// its author while they head a unit; a draft shared block its author while it applies to a unit of the author's subtrees. A level's fields
    /// change with the rights to its track.
    /// </summary>
    /// <param name="entry">An entry the user can read; a level with its track loaded.</param>
    /// <returns>True when the user may edit it.</returns>
    public bool CanEdit(CatalogEntry entry) => entry switch
    {
        CareerLevel level => CanEdit(level.Track),
        CompetencyBlock block => Keeps(unitId: null)
            || (block.Status == CatalogStatus.Draft && block.AuthorId == UserId && HeadsSubtreeOf(block.ScopeUnitId)),
        _ => Keeps(entry.UnitId) || (entry.Status == CatalogStatus.Draft && Heads(entry.UnitId) && (entry.UnitId is not null || entry.AuthorId == UserId)),
    };

    /// <summary>
    /// Whether the user may delete the entry: whoever may edit it, and for a draft also its author.
    /// </summary>
    /// <param name="entry">An entry the user can read; a level with its track loaded.</param>
    /// <returns>True when the user may delete it.</returns>
    public bool CanDelete(CatalogEntry entry) => CanEdit(entry) || (entry.Status == CatalogStatus.Draft && entry.AuthorId == UserId);

    /// <summary>
    /// Whether the user may approve, deactivate and reactivate the entry: global administrators, the responsible HR of the entry's unit, and every HR for an entry of no unit.
    /// </summary>
    /// <param name="entry">An entry the user can read; a level with its track loaded.</param>
    /// <returns>True when the user keeps the entry.</returns>
    public bool CanManage(CatalogEntry entry) => Keeps(entry.UnitId);

    /// <summary>
    /// Whether the user's right lets a draft become active together with an entry or version published by it: the right to manage the
    /// draft, except that a shared block that applies to a unit is approved this way only by global administrators and the unit's
    /// responsible HR.
    /// </summary>
    /// <param name="entry">A draft in a publication.</param>
    /// <returns>True when the user may approve it with the publication.</returns>
    public bool MayPublishWith(CatalogEntry entry) => entry is CompetencyBlock { ScopeUnitId: { } scopeUnitId }
        ? IsAdmin || HrUnitIds.Contains(scopeUnitId)
        : CanManage(entry);

    /// <summary>
    /// The status a new shared block gets from this user: active from global administrators and every HR, a draft from a unit head when
    /// it applies to a unit of the head's subtrees.
    /// </summary>
    /// <param name="scopeUnitId">The unit the block is to apply to; null for all.</param>
    /// <returns>The status, or <see langword="null"/> when the user may not create the block.</returns>
    public CatalogStatus? StatusOfNewBlock(Guid? scopeUnitId) => Keeps(unitId: null)
        ? CatalogStatus.Active
        : HeadsSubtreeOf(scopeUnitId) ? CatalogStatus.Draft : null;

    /// <summary>
    /// Whether a unit head may let a shared block apply to the unit: one of the units the user heads or a unit below them.
    /// </summary>
    /// <param name="scopeUnitId">The unit; null for all, which no head may choose.</param>
    /// <returns>True when the unit is in the user's subtrees.</returns>
    public bool HeadsSubtreeOf(Guid? scopeUnitId) => scopeUnitId is { } id && HeadedSubtreeIds.Contains(id);

    /// <summary>
    /// The status a new entry gets from this user: active from its keepers, a draft from the head of its unit, or for an entry of no unit
    /// from any unit head.
    /// </summary>
    /// <param name="unitId">The unit of the new track; null for an entry of no unit.</param>
    /// <returns>The status, or <see langword="null"/> when the user may not create the entry.</returns>
    public CatalogStatus? StatusOfNew(Guid? unitId) => Keeps(unitId)
        ? CatalogStatus.Active
        : Heads(unitId) ? CatalogStatus.Draft : null;

    private Func<CatalogEntry, bool> Compiled(CatalogEntry entry) => entry switch
    {
        CareerDirection => Compiled<CareerDirection>(),
        CareerTrack => Compiled<CareerTrack>(),
        CareerLevel => Compiled<CareerLevel>(),
        CareerCompetency => Compiled<CareerCompetency>(),
        CompetencyScale => Compiled<CompetencyScale>(),
        CompetencyBlock => Compiled<CompetencyBlock>(),
        _ => throw new UnreachableException($"Unknown catalogue entry type {entry.GetType().Name}."),
    };

    private Func<CatalogEntry, bool> Compiled<T>()
        where T : CatalogEntry
    {
        var sees = Sees<T>().Compile();
        return entry => sees((T)entry);
    }

    private bool Keeps(Guid? unitId) => IsAdmin || (unitId is { } id ? HrUnitIds.Contains(id) : IsHr);

    private bool Heads(Guid? unitId) => unitId is { } id ? HeadedUnitIds.Contains(id) : HeadedUnitIds.Count > 0;
}
