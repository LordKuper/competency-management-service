namespace Kalibr.Promotion;

/// <summary>
/// The employees and positions that cannot be named as raters.
/// </summary>
/// <param name="Employees">The excluded employees, by name.</param>
/// <param name="Positions">The excluded positions, by name; everyone who holds one is excluded.</param>
internal sealed record RaterExclusionsResponse(IReadOnlyList<ExcludedEmployeeResponse> Employees, IReadOnlyList<ExcludedPositionResponse> Positions);
