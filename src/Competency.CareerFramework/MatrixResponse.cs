namespace Competency.CareerFramework;

/// <summary>
/// A competency matrix as the API returns it, with its published version and, for those who may read it, its version in work. A track
/// matrix names its direction and track, a position matrix its position; the fields of the other kind are null.
/// </summary>
internal sealed record MatrixResponse(
    Guid Id,
    Guid OrgUnitId,
    string OrgUnitName,
    Guid? DirectionId,
    string? DirectionName,
    Guid? TrackId,
    string? TrackName,
    MatrixVersionRef? PublishedVersion,
    MatrixVersionRef? InWorkVersion,
    DateTimeOffset CreatedAt,
    Guid? PositionId = null,
    string? PositionName = null);
