using Competency.OrgStructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Competency.CareerFramework;

/// <summary>
/// Whether a track may be active: it has levels other than inactive ones, and each of them has a linked active position.
/// </summary>
/// <param name="HasLevels">Whether the track has a level that is not inactive.</param>
/// <param name="LevelsWithoutActivePosition">The levels that are not inactive and have no linked active position, in track order, each with its track.</param>
internal sealed record TrackReadiness(bool HasLevels, IReadOnlyList<CareerLevel> LevelsWithoutActivePosition)
{
    /// <summary>
    /// Whether the track may be active.
    /// </summary>
    public bool IsReady => HasLevels && LevelsWithoutActivePosition.Count == 0;

    /// <summary>
    /// Whether any of a level's linked positions is active; inactive positions stay linked but do not count.
    /// </summary>
    /// <param name="positionIds">The positions linked to the level.</param>
    /// <param name="positions">The catalogue positions by identifier.</param>
    /// <returns>True when the level has a linked active position.</returns>
    public static bool HasActivePosition(IEnumerable<Guid> positionIds, IReadOnlyDictionary<Guid, PositionStatus> positions) =>
        positionIds.Any(id => positions.GetValueOrDefault(id)?.IsActive == true);

    /// <summary>
    /// The refusal to make the track active, listing what keeps it from being ready.
    /// </summary>
    /// <param name="track">The track.</param>
    /// <returns>A 409 response.</returns>
    public ProblemHttpResult Refuse(CareerTrack track) => HasLevels
        ? PublicationRefusal.Refuse(
            $"Трек «{track.Name}» нельзя сделать действующим: у уровней нет связанной действующей должности",
            [.. LevelsWithoutActivePosition.Select(level => new PublicationBlocker(
                PublicationBlockerKind.Level, level.Id, level.Name, PublicationBlockerReason.NoActivePosition, track.Name))])
        : PublicationRefusal.Refuse(
            $"Трек «{track.Name}» нельзя сделать действующим: у него нет уровней",
            [new PublicationBlocker(PublicationBlockerKind.Track, track.Id, track.Name, PublicationBlockerReason.NoLevels, null)]);
}
