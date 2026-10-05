namespace Competency.UserManagement;

/// <summary>
/// The fields of an existing account, all replaced at once so that omitting one never changes it by accident;
/// this covers a rename, a change of role and binding or unbinding an employee. Blocking and passwords have their own operations.
/// </summary>
internal sealed record UpdateUserRequest
{
    public required string UserName { get; init; }

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
        EmployeeBinding.CheckShape(errors, Role, EmployeeId);
        return errors;
    }
}
