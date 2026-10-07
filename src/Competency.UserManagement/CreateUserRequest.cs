namespace Competency.UserManagement;

/// <summary>
/// The fields of a new account. A user may have an employee; an administrator must not, so <c>employeeId</c> is sent as null for that role.
/// </summary>
internal sealed record CreateUserRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

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
        CredentialChecks.CheckPassword(errors, "password", Password);
        EmployeeBinding.CheckShape(errors, Role, EmployeeId);
        return errors;
    }
}
