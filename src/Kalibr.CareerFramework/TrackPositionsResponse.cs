namespace Kalibr.CareerFramework;

/// <summary>
/// The levels of a track with the positions linked to them, for the screens that link positions to levels.
/// </summary>
/// <param name="CanEdit">Whether the signed-in user may link and unlink positions of the track's levels now.</param>
/// <param name="Levels">The levels the user can read, in track order.</param>
internal sealed record TrackPositionsResponse(bool CanEdit, IReadOnlyList<TrackLevelPositionsResponse> Levels);
