namespace Competency.CareerFramework;

/// <summary>
/// A career level as the API returns it, without its salary range, which only some readers get, from the salary endpoint.
/// The direction is the track's. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record CareerLevelResponse(
    Guid Id,
    string Name,
    Guid DirectionId,
    Guid TrackId,
    string Code,
    bool IsManagerial,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? RoleDescription,
    Guid? PreviousLevelId,
    IReadOnlyList<Guid> NextLevelIds,
    CatalogStatus Status,
    Guid? OwnerOrgUnitId,
    string? OwnerOrgUnitName,
    int Version)
{
    public static CareerLevelResponse From(CareerLevel level, string? ownerOrgUnitName) => new(
        level.Id,
        level.Name,
        level.Track.DirectionId,
        level.TrackId,
        level.Code,
        level.IsManagerial,
        level.ValidFrom,
        level.ValidTo,
        level.RoleDescription,
        level.PreviousLevelId,
        level.NextLevelIds,
        level.Status,
        level.OwnerOrgUnitId,
        ownerOrgUnitName,
        level.Version);
}
