using Kalibr.Platform;

namespace Kalibr.CareerFramework;

/// <summary>
/// A career level, a part of one track and kept by the track's unit. The track never changes, because matrices are built from a track's levels.
/// </summary>
[Audited]
internal sealed class CareerLevel : CatalogEntry
{
    [Audited]
    public Guid TrackId { get; init; }

    public CareerTrack Track { get; private set; } = null!;

    /// <summary>
    /// The level's place in its track, the most junior level lowest; unique within the track, with gaps where levels were deleted.
    /// </summary>
    [Audited]
    public int Position { get; set; }

    /// <inheritdoc />
    public override Guid? UnitId => Track.OwnerOrgUnitId;
}
