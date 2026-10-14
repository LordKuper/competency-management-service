using System.ComponentModel;

namespace Competency.CareerFramework;

/// <summary>
/// A new matrix of a unit, for a track of a direction or for a position; none of them changes afterwards. A track matrix names the
/// direction and the track, a position matrix the position alone.
/// </summary>
internal sealed record NewMatrixRequest
{
    public required Guid OrgUnitId { get; init; }

    /// <summary>
    /// The direction of a track matrix; absent for a position matrix.
    /// </summary>
    public Guid? DirectionId { get; init; }

    /// <summary>
    /// The track of a track matrix; absent for a position matrix.
    /// </summary>
    public Guid? TrackId { get; init; }

    /// <summary>
    /// The position of a position matrix; absent for a track matrix.
    /// </summary>
    public Guid? PositionId { get; init; }

    /// <summary>
    /// Accepted and not read: a matrix has no scale.
    /// </summary>
    [Description("Deprecated, removed in the next major version: accepted and ignored, a matrix has no scale.")]
    public Guid? ScaleId { get; init; }
}
