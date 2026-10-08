namespace Competency.UserManagement;

/// <summary>
/// An invited user setting their first password with the token from the invitation link.
/// </summary>
internal sealed record AcceptInvitationRequest
{
    public required string Token { get; init; }

    public required string Password { get; init; }

    /// <summary>
    /// Checks the shape of the password; the password policy is applied when the password is set, and the token when it is looked up.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CredentialChecks.CheckPassword(errors, "password", Password);
        return errors;
    }
}
