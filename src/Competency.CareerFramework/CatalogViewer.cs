using System.Diagnostics;
using System.Linq.Expressions;
using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// What the signed-in user may see and do in the career catalogues, from their account role and their process roles at the moment of asking.
/// Global administrators and every HR keep the entries no unit keeps, and a unit head proposes drafts of them. A track is kept by global
/// administrators and the responsible HR of its unit, and its unit's head edits it while it is a draft; a level goes with its track.
/// Every HR sees active and inactive entries. A draft of no unit is seen by its author, every HR and global administrators; a draft track or
/// level by its author, the head and the responsible HR of its unit and global administrators. Whoever reads a matrix version in work also
/// reads the drafts it uses. Everyone sees active entries.
/// </summary>
/// <param name="UserId">The signed-in account.</param>
/// <param name="IsAdmin">Whether the account is a global administrator.</param>
/// <param name="HeadedUnitIds">The units the account heads.</param>
/// <param name="HrUnitIds">The units the account is the responsible HR of.</param>
internal sealed record CatalogViewer(Guid UserId, bool IsAdmin, IReadOnlySet<Guid> HeadedUnitIds, IReadOnlySet<Guid> HrUnitIds)
{
    /// <summary>
    /// The entries of the matrix versions in work the user reads through the approval route rather than as a keeper or head of their unit.
    /// </summary>
    public IReadOnlySet<Guid> RouteEntryIds { get; init; } = new HashSet<Guid>();

    /// <summary>
    /// Whether the user is an HR: the responsible HR of at least one unit.
    /// </summary>
    public bool IsHr => HrUnitIds.Count > 0;

    /// <summary>
    /// Works out the signed-in user's rights; a writer calls it under the catalogue lock, so the rights hold until it commits.
    /// </summary>
    /// <param name="actor">The signed-in user.</param>
    /// <param name="roles">The process roles service.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The user's rights.</returns>
    public static Task<CatalogViewer> LoadAsync(ICurrentActor actor, ProcessRoles roles, CancellationToken cancellationToken) =>
        LoadAsync(actor.UserId ?? Guid.Empty, actor.Role == PlatformClaims.GlobalAdminRole, roles, cancellationToken);

    /// <summary>
    /// Works out the rights of an account other than the signed-in one, such as the responsible HR whose right publishes a matrix version.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="isAdmin">Whether the account is a global administrator.</param>
    /// <param name="roles">The process roles service.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The account's rights.</returns>
    public static async Task<CatalogViewer> LoadAsync(Guid userId, bool isAdmin, ProcessRoles roles, CancellationToken cancellationToken)
    {
        var headedUnits = await roles.HeadedUnitsAsync(userId, cancellationToken);
        var hrUnits = await roles.HrUnitsAsync(userId, cancellationToken);
        return new CatalogViewer(
            userId,
            isAdmin,
            headedUnits.Select(unit => unit.Id).ToHashSet(),
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
        LambdaExpression sees = typeof(T) == typeof(CareerTrack)
            ? (Expression<Func<CareerTrack, bool>>)(track => track.Status == CatalogStatus.Active
                || (track.Status == CatalogStatus.Inactive && seesInactive)
                || (track.Status == CatalogStatus.Draft && (isAdmin || track.AuthorId == userId || unitIds.Contains(track.OwnerOrgUnitId))))
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
    /// Whether the user may read an entry already loaded, by the same filter as <see cref="Sees{T}"/>.
    /// </summary>
    /// <param name="entry">The entry; a level with its track loaded.</param>
    /// <returns>True when the user may read it.</returns>
    public bool CanSee(CatalogEntry entry) => entry switch
    {
        CareerDirection direction => SeesEntry(direction),
        CareerTrack track => SeesEntry(track),
        CareerLevel level => SeesEntry(level),
        CompetencyCategory category => SeesEntry(category),
        CareerCompetency competency => SeesEntry(competency),
        CompetencyScale scale => SeesEntry(scale),
        CompetencyBlock block => SeesEntry(block),
        _ => throw new UnreachableException($"Unknown catalogue entry type {entry.GetType().Name}."),
    };

    /// <summary>
    /// Whether the user may change the entry's fields: its keepers, and while it is a draft the head of its unit, or for an entry of no unit
    /// its author while they head a unit. A level's fields change with the rights to its track.
    /// </summary>
    /// <param name="entry">An entry the user can read; a level with its track loaded.</param>
    /// <returns>True when the user may edit it.</returns>
    public bool CanEdit(CatalogEntry entry) => entry is CareerLevel level
        ? CanEdit(level.Track)
        : Keeps(entry.UnitId) || (entry.Status == CatalogStatus.Draft && Heads(entry.UnitId) && (entry.UnitId is not null || entry.AuthorId == UserId));

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
    /// The status a new entry gets from this user: active from its keepers, a draft from the head of its unit, or for an entry of no unit
    /// from any unit head.
    /// </summary>
    /// <param name="unitId">The unit of the new track; null for an entry of no unit.</param>
    /// <returns>The status, or <see langword="null"/> when the user may not create the entry.</returns>
    public CatalogStatus? StatusOfNew(Guid? unitId) => Keeps(unitId)
        ? CatalogStatus.Active
        : Heads(unitId) ? CatalogStatus.Draft : null;

    private bool SeesEntry<T>(T entry)
        where T : CatalogEntry => Sees<T>().Compile()(entry);

    private bool Keeps(Guid? unitId) => IsAdmin || (unitId is { } id ? HrUnitIds.Contains(id) : IsHr);

    private bool Heads(Guid? unitId) => unitId is { } id ? HeadedUnitIds.Contains(id) : HeadedUnitIds.Count > 0;
}
