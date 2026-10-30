namespace Competency.CareerFramework;

/// <summary>
/// A level of a track with its linked positions. The version is the level's, sent back in <c>If-Match</c> to link or unlink a position.
/// </summary>
/// <param name="Id">The level.</param>
/// <param name="Name">The level's name.</param>
/// <param name="Status">The level's status.</param>
/// <param name="Version">The level's version.</param>
/// <param name="HasActivePosition">Whether a linked position is active; inactive positions stay linked but do not count.</param>
/// <param name="Positions">The linked positions in name order.</param>
internal sealed record TrackLevelPositionsResponse(
    Guid Id,
    string Name,
    CatalogStatus Status,
    int Version,
    bool HasActivePosition,
    IReadOnlyList<LinkedPositionResponse> Positions);
