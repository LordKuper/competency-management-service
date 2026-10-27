namespace Competency.CareerFramework;

/// <summary>
/// The published version of a unit's track matrix with the levels of the track.
/// </summary>
/// <param name="UnitId">The unit the matrix is built for.</param>
/// <param name="MatrixId">The matrix identifier.</param>
/// <param name="VersionId">The published version identifier.</param>
/// <param name="VersionNumber">The version number within the matrix.</param>
/// <param name="TrackId">The career track.</param>
/// <param name="TrackName">The track name as the version froze it.</param>
/// <param name="Levels">The levels of the version, the most junior first.</param>
public sealed record PublishedTrackMatrix(
    Guid UnitId,
    Guid MatrixId,
    Guid VersionId,
    int VersionNumber,
    Guid TrackId,
    string TrackName,
    IReadOnlyList<CareerVersionLevel> Levels);
