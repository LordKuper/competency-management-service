using System.Diagnostics;

namespace Competency.UserManagement;

/// <summary>
/// The fixed identifiers of the Identity role rows, one per <see cref="UserRole"/> member; a new member needs its identifier here and a migration adding its row.
/// </summary>
internal static class UserRoleIds
{
    public static Guid Of(UserRole role) => role switch
    {
        UserRole.GlobalAdmin => new("0991bde9-ccde-4831-a138-eef704ed876c"),
        UserRole.Hr => new("2d465e5b-a4c6-46af-bae3-926cca129e5e"),
        _ => throw new UnreachableException($"Role {role} has no row."),
    };

    /// <summary>
    /// The roles whose rows have the given identifiers, in <see cref="UserRole"/> member order; usable in a query's final projection.
    /// </summary>
    /// <param name="roleIds">The identifiers of the account's role rows.</param>
    /// <returns>The roles.</returns>
    public static IReadOnlyList<UserRole> RolesOf(IEnumerable<Guid> roleIds) =>
        [.. Enum.GetValues<UserRole>().Where(role => roleIds.Contains(Of(role)))];
}
