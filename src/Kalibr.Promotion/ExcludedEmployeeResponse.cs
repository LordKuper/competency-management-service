namespace Kalibr.Promotion;

/// <summary>
/// An employee who cannot be named as a rater.
/// </summary>
/// <param name="Id">The employee.</param>
/// <param name="FullName">The employee's full name.</param>
/// <param name="UnitName">The unit the employee works in.</param>
internal sealed record ExcludedEmployeeResponse(Guid Id, string FullName, string UnitName);
