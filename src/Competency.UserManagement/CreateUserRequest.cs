namespace Competency.UserManagement;

/// <summary>
/// The fields of a new account. A user needs an employee; an administrator must not have one, so <c>employeeId</c> is sent as null for that role.
/// </summary>
internal sealed record CreateUserRequest
{
    public required string UserName { get; init; }

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
        CredentialChecks.CheckUserName(errors, "userName", UserName);
        CredentialChecks.CheckPassword(errors, "password", Password);
        EmployeeBinding.CheckShape(errors, Role, EmployeeId);
        return errors;
    }
}
