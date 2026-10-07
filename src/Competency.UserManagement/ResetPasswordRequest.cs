namespace Competency.UserManagement;

/// <summary>
/// An administrator replacing a user's password, which ends all of that user's sessions.
/// </summary>
internal sealed record ResetPasswordRequest
{
    public required string NewPassword { get; init; }

    /// <summary>
    /// Checks the shape of the password; the password policy is applied when the password is set.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CredentialChecks.CheckPassword(errors, "newPassword", NewPassword);
        return errors;
    }
}
