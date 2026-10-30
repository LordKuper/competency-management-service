namespace Kalibr.CareerFramework;

/// <summary>
/// A new career level: its name and track, which never changes afterwards.
/// </summary>
internal sealed record NewCareerLevelRequest : CatalogNameRequest
{
    public required Guid TrackId { get; init; }
}
