namespace Competency.UserManagement;

/// <summary>
/// A user replacing their own password, proving they know the current one.
/// </summary>
internal sealed record ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }

    public required string NewPassword { get; init; }

    /// <summary>
    /// Checks the shape of both passwords; the password policy is applied when the password is set.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CredentialChecks.CheckPassword(errors, "currentPassword", CurrentPassword);
        CredentialChecks.CheckPassword(errors, "newPassword", NewPassword);
        return errors;
    }
}
