namespace Kalibr.Promotion;

/// <summary>
/// The level of a track an application aims at.
/// </summary>
/// <param name="UnitId">The unit whose matrix it is; its commission reviews the application.</param>
/// <param name="UnitName">The unit name.</param>
/// <param name="MatrixId">The matrix of the unit.</param>
/// <param name="TrackId">The career track.</param>
/// <param name="TrackName">The track name as the bound version froze it.</param>
/// <param name="LevelId">The level.</param>
/// <param name="LevelName">The level name as the bound version froze it.</param>
internal sealed record ApplicationTargetResponse(Guid UnitId, string UnitName, Guid MatrixId, Guid TrackId, string TrackName, Guid LevelId, string LevelName);
