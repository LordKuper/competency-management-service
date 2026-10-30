namespace Kalibr.CareerFramework;

/// <summary>
/// A career level as the API returns it. The direction and the unit are the track's. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// <c>CanEdit</c> and <c>CanDelete</c> say whether the signed-in user may change and delete the entry now, so a client offers only what the server allows.
/// </summary>
internal sealed record CareerLevelResponse(
    Guid Id,
    string Name,
    Guid DirectionId,
    Guid TrackId,
    int Position,
    CatalogStatus Status,
    int Version,
    bool CanEdit,
    bool CanDelete)
{
    public static CareerLevelResponse From(CareerLevel level, CatalogViewer viewer) => new(
        level.Id,
        level.Name,
        level.Track.DirectionId,
        level.TrackId,
        level.Position,
        level.Status,
        level.Version,
        viewer.CanEdit(level),
        viewer.CanDelete(level));
}
