namespace Competency.UserManagement;

/// <summary>
/// The password rule the interface can state before a password is typed: the shortest length the server accepts.
/// </summary>
internal sealed record PasswordPolicyResponse(int MinLength);
