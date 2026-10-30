namespace Kalibr.CareerFramework;

/// <summary>
/// A position a unit may hold, as the choice of the position of a new position matrix lists it.
/// </summary>
/// <param name="Id">The position.</param>
/// <param name="Name">The position's name.</param>
/// <param name="IsLinked">Whether a level of a track, in any status, stands for the position, so it can have no position matrix.</param>
/// <param name="LinkedTrack">The first such track by name the user can read; null when there is none.</param>
internal sealed record MatrixPositionResponse(Guid Id, string Name, bool IsLinked, CareerNamedResponse? LinkedTrack);
