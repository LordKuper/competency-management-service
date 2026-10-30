namespace Kalibr.CareerFramework;

/// <summary>
/// A level above the user's own on a track.
/// </summary>
/// <param name="Id">The level.</param>
/// <param name="Name">The level's name.</param>
/// <param name="Requirements">What the published matrix of the user's unit requires at the level; null when the unit has no published
/// matrix for the track.</param>
internal sealed record MyPlaceLevelResponse(Guid Id, string Name, IReadOnlyList<MyPlaceRequirementResponse>? Requirements);
