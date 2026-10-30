namespace Kalibr.OrgStructure;

/// <summary>
/// The facts about a catalogue position that other modules may rely on.
/// </summary>
/// <param name="Id">The position identifier.</param>
/// <param name="Name">The position name.</param>
/// <param name="IsActive">Whether the position may still be given to employees rather than deactivated.</param>
public sealed record PositionStatus(Guid Id, string Name, bool IsActive);
