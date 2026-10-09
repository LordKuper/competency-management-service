namespace Competency.CareerFramework;

/// <summary>
/// A new matrix of a unit for a track, with the scale of its first version; none of them changes afterwards.
/// </summary>
internal sealed record NewMatrixRequest
{
    public required Guid OrgUnitId { get; init; }

    public required Guid DirectionId { get; init; }

    public required Guid TrackId { get; init; }

    public required Guid ScaleId { get; init; }
}
