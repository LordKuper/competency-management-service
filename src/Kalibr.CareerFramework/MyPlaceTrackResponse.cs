namespace Competency.CareerFramework;

/// <summary>
/// A career track the user's position is on, with the user's level, worked out from the position, and the levels above it.
/// </summary>
/// <param name="Track">The track.</param>
/// <param name="Direction">The track's direction.</param>
/// <param name="CurrentLevel">The level the user's position is linked to.</param>
/// <param name="Matrix">The published matrix version of the user's unit for the track, which the requirements come from; null when there is none.</param>
/// <param name="NextLevels">The active levels above the current one, in track order.</param>
internal sealed record MyPlaceTrackResponse(
    CareerNamedResponse Track,
    CareerNamedResponse Direction,
    CareerNamedResponse CurrentLevel,
    MyPlaceMatrixResponse? Matrix,
    IReadOnlyList<MyPlaceLevelResponse> NextLevels);
