using Kalibr.Platform;

namespace Kalibr.CareerFramework;

/// <summary>
/// The competency matrix of one unit, built either for a career track of a direction or for a position without a track; a unit has at
/// most one per track and one per position. Its content lives in its versions. None of the columns ever changes, because every version
/// is built for them.
/// </summary>
[Audited]
internal sealed class CompetencyMatrix : EntityBase
{
    [Audited]
    public Guid OrgUnitId { get; init; }

    /// <summary>
    /// The direction of a track matrix; null for a position matrix.
    /// </summary>
    [Audited]
    public Guid? DirectionId { get; init; }

    /// <summary>
    /// The track of a track matrix; null for a position matrix.
    /// </summary>
    [Audited]
    public Guid? TrackId { get; init; }

    /// <summary>
    /// The position of a position matrix, whose one requirements column is keyed by it; null for a track matrix.
    /// </summary>
    [Audited]
    public Guid? PositionId { get; init; }
}
