namespace Competency.Promotion;

/// <summary>
/// An employee asked to rate a candidate.
/// </summary>
/// <param name="EmployeeId">The employee.</param>
/// <param name="FullName">The employee's full name.</param>
/// <param name="Source">Who put the employee on the list.</param>
/// <param name="HasAccount">Whether the employee has an account; one without gets nothing unless the address below is confirmed.</param>
/// <param name="Email">The address the invitation goes to; null for an employee who has an account.</param>
/// <param name="IsEmailConfirmed">Whether the head of the unit of the target or the responsible HR confirmed the address.</param>
/// <param name="EmailConfirmedByName">Who confirmed the address, as people know the account; null while it is not confirmed.</param>
internal sealed record RaterResponse(
    Guid EmployeeId,
    string FullName,
    RaterSource Source,
    bool HasAccount,
    string? Email,
    bool IsEmailConfirmed,
    string? EmailConfirmedByName);
