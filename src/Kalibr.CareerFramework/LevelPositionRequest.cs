namespace Competency.CareerFramework;

/// <summary>
/// The position to link to a level.
/// </summary>
internal sealed record LevelPositionRequest
{
    public required Guid PositionId { get; init; }
}
