namespace Competency.UserManagement;

/// <summary>
/// The credentials of a sign-in attempt.
/// </summary>
internal sealed record LoginRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    /// <summary>
    /// Checks the shape of the credentials; whether they are right is not revealed until the attempt is made.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CredentialChecks.CheckEmail(errors, "email", Email);
        CredentialChecks.CheckPassword(errors, "password", Password);
        return errors;
    }
}
