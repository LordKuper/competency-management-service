namespace Kalibr.CareerFramework;

/// <summary>
/// Why a draft track may not be approved yet: it has no levels, or levels without a linked active position.
/// </summary>
/// <param name="HasLevels">Whether the track has a level that is not inactive.</param>
/// <param name="LevelsWithoutActivePosition">Those levels without a linked active position, in track order.</param>
internal sealed record TrackReadinessResponse(bool HasLevels, IReadOnlyList<CareerNamedResponse> LevelsWithoutActivePosition)
{
    /// <summary>
    /// Describes what keeps a track from being ready.
    /// </summary>
    /// <param name="readiness">The track's readiness.</param>
    /// <returns>The description, or <see langword="null"/> when the track is ready.</returns>
    public static TrackReadinessResponse? Of(TrackReadiness readiness) => readiness.IsReady
        ? null
        : new(readiness.HasLevels, [.. readiness.LevelsWithoutActivePosition.Select(level => new CareerNamedResponse(level.Id, level.Name))]);
}
