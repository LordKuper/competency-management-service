namespace Competency.CareerFramework;

/// <summary>
/// The positions a unit's employees may hold.
/// </summary>
/// <param name="Own">The positions on the unit's own list in name order.</param>
/// <param name="Inherited">The positions on the lists of the units above it and not on its own, in name order.</param>
internal sealed record UnitPositionsResponse(IReadOnlyList<UnitPositionResponse> Own, IReadOnlyList<UnitPositionResponse> Inherited);
