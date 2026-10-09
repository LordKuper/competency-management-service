using System.Linq.Expressions;
using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// What the signed-in user may see and do in the career catalogues, from their account role and their process roles at the moment of asking.
/// Global administrators see and keep every entry. A responsible HR sees active and inactive entries of every unit and drafts of the units
/// they answer for, and keeps those units' entries. A unit head sees and edits their unit's drafts. An author always sees their own drafts.
/// Whoever reads a matrix version in work also reads the drafts it uses. Everyone sees active entries.
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
    /// Works out the signed-in user's rights; a writer calls it under the catalogue lock, so the rights hold until it commits.
    /// </summary>
    /// <param name="actor">The signed-in user.</param>
    /// <param name="roles">The process roles service.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The user's rights.</returns>
    public static async Task<CatalogViewer> LoadAsync(ICurrentActor actor, ProcessRoles roles, CancellationToken cancellationToken)
    {
        var userId = actor.UserId ?? Guid.Empty;
        var headedUnits = await roles.HeadedUnitsAsync(userId, cancellationToken);
        var hrUnits = await roles.HrUnitsAsync(userId, cancellationToken);
        return new CatalogViewer(
            userId,
            actor.Role == PlatformClaims.GlobalAdminRole,
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
        var seesInactive = IsAdmin || HrUnitIds.Count > 0;
        var userId = UserId;
        var draftUnitIds = HeadedUnitIds.Union(HrUnitIds).Select(id => (Guid?)id).ToList();
        var routeEntryIds = RouteEntryIds.ToList();
        return entry => entry.Status == CatalogStatus.Active
            || (entry.Status == CatalogStatus.Inactive && seesInactive)
            || (entry.Status == CatalogStatus.Draft
                && (isAdmin || entry.AuthorId == userId || draftUnitIds.Contains(entry.OwnerOrgUnitId) || routeEntryIds.Contains(entry.Id)));
    }

    /// <summary>
    /// Whether the user may change the entry's fields: the keepers of its unit, and the unit's head while the entry is a draft.
    /// </summary>
    /// <param name="entry">An entry the user can read.</param>
    /// <returns>True when the user may edit it.</returns>
    public bool CanEdit(CatalogEntry entry) =>
        Keeps(entry.OwnerOrgUnitId) || (entry.Status == CatalogStatus.Draft && IsHeadOf(entry.OwnerOrgUnitId));

    /// <summary>
    /// Whether the user may approve, deactivate and reactivate the entry, and read and change a level's salary range:
    /// global administrators, and the responsible HR of the entry's unit.
    /// </summary>
    /// <param name="entry">An entry the user can read.</param>
    /// <returns>True when the user keeps the entry.</returns>
    public bool CanManage(CatalogEntry entry) => Keeps(entry.OwnerOrgUnitId);

    /// <summary>
    /// The status a new entry of the unit gets from this user: active from its keepers, a draft from its head.
    /// </summary>
    /// <param name="ownerOrgUnitId">The unit the entry is for; null for a shared entry.</param>
    /// <returns>The status, or <see langword="null"/> when the user may not create the entry.</returns>
    public CatalogStatus? StatusOfNew(Guid? ownerOrgUnitId) => Keeps(ownerOrgUnitId)
        ? CatalogStatus.Active
        : IsHeadOf(ownerOrgUnitId) ? CatalogStatus.Draft : null;

    private bool Keeps(Guid? ownerOrgUnitId) => IsAdmin || (ownerOrgUnitId is { } unitId && HrUnitIds.Contains(unitId));

    private bool IsHeadOf(Guid? ownerOrgUnitId) => ownerOrgUnitId is { } unitId && HeadedUnitIds.Contains(unitId);
}
