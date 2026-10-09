namespace Competency.CareerFramework;

/// <summary>
/// The account to assign as a unit's HR; it must be able to act.
/// </summary>
internal sealed record OrgUnitHrRequest
{
    public required Guid UserId { get; init; }
}
