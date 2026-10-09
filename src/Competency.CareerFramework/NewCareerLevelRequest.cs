namespace Competency.CareerFramework;

/// <summary>
/// A new career level: its fields, unit and track, of which the unit and the track never change afterwards.
/// </summary>
internal sealed record NewCareerLevelRequest : CareerLevelRequest
{
    /// <summary>
    /// The unit that keeps the level; null for a shared level.
    /// </summary>
    public required Guid? OwnerOrgUnitId { get; init; }

    public required Guid TrackId { get; init; }
}
