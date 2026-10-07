using System.Linq.Expressions;

namespace Competency.UserManagement;

/// <summary>
/// An account as administrators see it. Credentials, stamps and lockout state are never part of it.
/// </summary>
internal sealed record UserResponse(
    Guid Id,
    string Email,
    UserRole Role,
    bool IsBlocked,
    Guid? EmployeeId,
    string? EmployeeName,
    int Version)
{
    public static readonly Expression<Func<AppUser, UserResponse>> Projection = user => new UserResponse(
        user.Id,
        user.Email!,
        user.Role,
        user.IsBlocked,
        user.EmployeeId,
        null,
        user.Version);

    private static readonly Func<AppUser, UserResponse> Compiled = Projection.Compile();

    public static UserResponse From(AppUser user, string? employeeName) => Compiled(user) with { EmployeeName = employeeName };
}
