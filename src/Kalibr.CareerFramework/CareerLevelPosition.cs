using Kalibr.Platform;

namespace Kalibr.CareerFramework;

/// <summary>
/// A position of the shared catalogue that a level of a track stands for. Within a track a position belongs to one level at most.
/// The track is the level's, kept on the row so the database can hold that rule. Removing the link deletes the row.
/// </summary>
[Audited]
internal sealed class CareerLevelPosition
{
    [Audited]
    public Guid LevelId { get; init; }

    [Audited]
    public Guid PositionId { get; init; }

    [Audited]
    public Guid TrackId { get; init; }
}
