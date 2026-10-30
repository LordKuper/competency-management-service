namespace Kalibr.CareerFramework;

/// <summary>
/// A position linked to a level of a track.
/// </summary>
/// <param name="Id">The position.</param>
/// <param name="Name">The position's name.</param>
/// <param name="IsActive">Whether the position may still be given to employees.</param>
/// <param name="EmployeeCount">The working employees holding the position in the track's unit and the units below it.</param>
/// <param name="CanUnlink">Whether the signed-in user may unlink it now, by the rule the unlinking itself applies.</param>
internal sealed record LinkedPositionResponse(Guid Id, string Name, bool IsActive, int EmployeeCount, bool CanUnlink);
