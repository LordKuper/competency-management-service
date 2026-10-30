namespace Kalibr.Promotion;

/// <summary>
/// An employee asked to rate a candidate.
/// </summary>
/// <param name="EmployeeId">The employee.</param>
/// <param name="FullName">The employee's full name.</param>
/// <param name="UnitName">The name of the unit the employee belongs to.</param>
/// <param name="IsFromOtherUnit">Whether the employee belongs to a unit other than the candidate's own, which the minimum of raters from other units counts.</param>
/// <param name="Source">Who put the employee on the list.</param>
/// <param name="HasAccount">Whether the employee has an account; one without gets nothing unless the address below is confirmed.</param>
/// <param name="Email">The address the invitation goes to; null for an employee who has an account.</param>
/// <param name="IsEmailConfirmed">Whether the head of the unit of the target or the responsible HR confirmed the address.</param>
/// <param name="EmailConfirmedByName">Who confirmed the address, as people know the account; null while it is not confirmed.</param>
/// <param name="IsExcluded">Whether the exclusion list names the employee or their position now; one listed before it did stays on the list, marked.</param>
internal sealed record RaterResponse(
    Guid EmployeeId,
    string FullName,
    string UnitName,
    bool IsFromOtherUnit,
    RaterSource Source,
    bool HasAccount,
    string? Email,
    bool IsEmailConfirmed,
    string? EmailConfirmedByName,
    bool IsExcluded);
