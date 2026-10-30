namespace Competency.CareerFramework;

/// <summary>
/// A track with its levels other than inactive ones, in track order, and every link of its levels to positions, read in one query for the desk.
/// </summary>
/// <param name="Track">The track.</param>
/// <param name="Levels">The levels other than inactive ones, in track order.</param>
/// <param name="Links">The links of the track's levels, inactive ones included.</param>
internal sealed record CareerDeskTrack(CareerTrack Track, List<CareerLevel> Levels, List<CareerLevelPosition> Links);
