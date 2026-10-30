namespace Competency.Promotion;

/// <summary>
/// The matrix of a track a candidate of an open wave can apply by, with the levels of the track the matrix covers.
/// </summary>
/// <param name="MatrixId">The matrix of the unit.</param>
/// <param name="TrackId">The career track.</param>
/// <param name="TrackName">The track name.</param>
/// <param name="Levels">The levels the candidate can apply for, the most junior first.</param>
internal sealed record WaveTargetResponse(Guid MatrixId, Guid TrackId, string TrackName, IReadOnlyList<WaveTargetLevelResponse> Levels);
