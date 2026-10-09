namespace Competency.CareerFramework;

/// <summary>
/// A career track as the API returns it. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record CareerTrackResponse(
    Guid Id,
    string Name,
    Guid DirectionId,
    CatalogStatus Status,
    Guid? OwnerOrgUnitId,
    string? OwnerOrgUnitName,
    int Version)
{
    public static CareerTrackResponse From(CareerTrack track, string? ownerOrgUnitName) =>
        new(track.Id, track.Name, track.DirectionId, track.Status, track.OwnerOrgUnitId, ownerOrgUnitName, track.Version);
}
