namespace Competency.CareerFramework;

/// <summary>
/// A career level as the API returns it. The direction and the unit are the track's. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record CareerLevelResponse(
    Guid Id,
    string Name,
    Guid DirectionId,
    Guid TrackId,
    int Position,
    CatalogStatus Status,
    int Version)
{
    public static CareerLevelResponse From(CareerLevel level) => new(
        level.Id,
        level.Name,
        level.Track.DirectionId,
        level.TrackId,
        level.Position,
        level.Status,
        level.Version);
}
