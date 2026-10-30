namespace Kalibr.OrgStructure;

/// <summary>
/// A catalogue position to allow in a unit; it must be active.
/// </summary>
internal sealed record OrgUnitPositionRequest
{
    public required Guid PositionId { get; init; }
}
