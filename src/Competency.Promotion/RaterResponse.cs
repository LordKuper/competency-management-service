namespace Competency.Promotion;

/// <summary>
/// An employee asked to rate a candidate.
/// </summary>
/// <param name="EmployeeId">The employee.</param>
/// <param name="FullName">The employee's full name.</param>
/// <param name="Source">Who put the employee on the list.</param>
internal sealed record RaterResponse(Guid EmployeeId, string FullName, RaterSource Source);
