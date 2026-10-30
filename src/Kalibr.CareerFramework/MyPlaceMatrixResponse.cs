namespace Kalibr.CareerFramework;

/// <summary>
/// A published matrix version of the user's unit, named by its track as it stood at publication.
/// </summary>
internal sealed record MyPlaceMatrixResponse(Guid MatrixId, Guid TrackId, string TrackName, Guid VersionId, int Number, DateTimeOffset? PublishedAt);
