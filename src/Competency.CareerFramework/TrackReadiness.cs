using Competency.OrgStructure;

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
    /// Works out the readiness of a track from its levels and their links read beforehand.
    /// </summary>
    /// <param name="levels">The track's levels other than inactive ones, in track order.</param>
    /// <param name="positionsByLevel">The positions linked to each level.</param>
    /// <param name="positions">The catalogue positions by identifier.</param>
    /// <returns>The track's readiness.</returns>
    public static TrackReadiness Of(IReadOnlyList<CareerLevel> levels, ILookup<Guid, Guid> positionsByLevel, IReadOnlyDictionary<Guid, PositionStatus> positions) =>
        new(levels.Count > 0, [.. levels.Where(level => !HasActivePosition(positionsByLevel[level.Id], positions))]);

    /// <summary>
    /// Whether any of a level's linked positions is active; inactive positions stay linked but do not count.
    /// </summary>
    /// <param name="positionIds">The positions linked to the level.</param>
    /// <param name="positions">The catalogue positions by identifier.</param>
    /// <returns>True when the level has a linked active position.</returns>
    public static bool HasActivePosition(IEnumerable<Guid> positionIds, IReadOnlyDictionary<Guid, PositionStatus> positions) =>
        positionIds.Any(id => positions.GetValueOrDefault(id)?.IsActive == true);

    /// <summary>
    /// What keeps the track from being ready, each with the entry it is about: the track without levels, or else its levels without an active position.
    /// </summary>
    /// <param name="track">The track.</param>
    /// <returns>The entries in the way; empty when the track is ready.</returns>
    public IEnumerable<(CatalogEntry Entry, PublicationBlocker Blocker)> Blockers(CareerTrack track) => HasLevels
        ? LevelsWithoutActivePosition.Select(level => ((CatalogEntry)level, new PublicationBlocker(
            PublicationEntryKind.Level, level.Id, level.Name, PublicationBlockerReason.NoActivePosition, track.Name)))
        : [(track, new PublicationBlocker(PublicationEntryKind.Track, track.Id, track.Name, PublicationBlockerReason.NoLevels, null))];
}
