namespace Competency.CareerFramework;

/// <summary>
/// A new career level: its fields and track, which never changes afterwards.
/// </summary>
internal sealed record NewCareerLevelRequest : CareerLevelRequest
{
    public required Guid TrackId { get; init; }
}
