namespace Kalibr.CareerFramework;

/// <summary>
/// The levels of a track in their new order, the most junior first.
/// </summary>
internal sealed record LevelOrderRequest
{
    public required IReadOnlyList<Guid> LevelIds { get; init; }
}
