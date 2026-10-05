namespace Competency.UserManagement;

/// <summary>
/// The credentials of a sign-in attempt.
/// </summary>
internal sealed record LoginRequest
{
    public required string UserName { get; init; }

    public required string Password { get; init; }

    /// <summary>
    /// Checks the shape of the credentials; whether they are right is not revealed until the attempt is made.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CredentialChecks.CheckUserName(errors, "userName", UserName);
        CredentialChecks.CheckPassword(errors, "password", Password);
        return errors;
    }
}
