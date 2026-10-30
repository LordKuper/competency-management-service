namespace Kalibr.CareerFramework;

/// <summary>
/// A competency matrix as the API returns it, with its published version and, for those who may read it, its version in work. A track
/// matrix names its direction and track, a position matrix its position; the fields of the other kind are null.
/// A matrix with a published version is in effect in its own unit and in the units below it without a published matrix of their own
/// for the same track or position; it is in effect nowhere before that.
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
    IReadOnlyList<CareerUnitResponse> EffectiveUnits,
    DateTimeOffset CreatedAt,
    Guid? PositionId = null,
    string? PositionName = null);
