namespace Kalibr.CareerFramework;

/// <summary>
/// A position on the list of a unit or of a unit above it.
/// </summary>
/// <param name="Id">The position.</param>
/// <param name="Name">The position's name.</param>
/// <param name="IsActive">Whether the position may still be given to employees.</param>
internal sealed record UnitPositionResponse(Guid Id, string Name, bool IsActive);
