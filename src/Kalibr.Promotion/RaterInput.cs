namespace Kalibr.Promotion;

/// <summary>
/// An employee proposed as a rater, and the address to invite them at when they have no account.
/// </summary>
/// <param name="EmployeeId">The employee.</param>
/// <param name="Email">The address for the invitation; ignored for an employee who has an account.</param>
internal sealed record RaterInput(Guid EmployeeId, string? Email);
