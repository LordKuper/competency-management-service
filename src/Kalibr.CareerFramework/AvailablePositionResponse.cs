namespace Kalibr.CareerFramework;

/// <summary>
/// An active position a level of a track may be linked to.
/// </summary>
/// <param name="Id">The position.</param>
/// <param name="Name">The position's name.</param>
/// <param name="EmployeeCount">The working employees holding the position in the track's unit and the units below it.</param>
internal sealed record AvailablePositionResponse(Guid Id, string Name, int EmployeeCount);
