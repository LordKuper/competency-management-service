namespace Competency.CareerFramework;

/// <summary>
/// The responsible HR of a unit.
/// </summary>
/// <param name="EmployeeId">The employee the HR assignment names.</param>
/// <param name="UserId">The account of that employee, which can act as an HR.</param>
public sealed record ResponsibleHr(Guid EmployeeId, Guid UserId);
