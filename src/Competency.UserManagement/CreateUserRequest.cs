namespace Competency.UserManagement;

/// <summary>
/// The fields of a new account, which is invited to set its own password by e-mail. An account of either role may have an employee; <c>employeeId</c> is null for none.
/// </summary>
internal sealed record CreateUserRequest
{
    public required string Email { get; init; }

    public required UserRole Role { get; init; }

    public required Guid? EmployeeId { get; init; }

    /// <summary>
    /// Checks the fields that need no lookup.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CredentialChecks.CheckEmail(errors, "email", Email);
        EmployeeBinding.CheckRole(errors, Role);
        return errors;
    }
}
