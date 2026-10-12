namespace Competency.CareerFramework;

/// <summary>
/// An account named in a matrix response by its employee's full name, or else by its e-mail.
/// </summary>
internal sealed record CareerPersonResponse(Guid UserId, string Name);
