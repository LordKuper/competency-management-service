namespace Competency.CareerFramework;

/// <summary>
/// A career track as the API returns it, with the unit that keeps it. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// <c>CanEdit</c> and <c>CanDelete</c> say whether the signed-in user may change and delete the entry now, so a client offers only what the server allows.
/// </summary>
internal sealed record CareerTrackResponse(
    Guid Id,
    string Name,
    Guid DirectionId,
    CatalogStatus Status,
    Guid OwnerOrgUnitId,
    string OwnerOrgUnitName,
    int Version,
    bool CanEdit,
    bool CanDelete)
{
    public static CareerTrackResponse From(CareerTrack track, string? ownerOrgUnitName, CatalogViewer viewer) => new(
        track.Id,
        track.Name,
        track.DirectionId,
        track.Status,
        track.OwnerOrgUnitId,
        ownerOrgUnitName ?? string.Empty,
        track.Version,
        viewer.CanEdit(track),
        viewer.CanDelete(track));
}
