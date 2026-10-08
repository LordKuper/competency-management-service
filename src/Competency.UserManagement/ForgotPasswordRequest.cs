namespace Competency.UserManagement;

/// <summary>
/// An anonymous request for a password reset link to the account with the given e-mail address.
/// </summary>
internal sealed record ForgotPasswordRequest
{
    public required string Email { get; init; }

    /// <summary>
    /// Checks the shape of the address only, so the answer never depends on whether an account has it.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CredentialChecks.CheckEmail(errors, "email", Email);
        return errors;
    }
}
