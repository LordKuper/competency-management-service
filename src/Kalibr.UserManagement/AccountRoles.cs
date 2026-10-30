using System.Text.Json;
using Competency.Platform;
using Microsoft.AspNetCore.Identity;

namespace Competency.UserManagement;

/// <summary>
/// An account's roles as Identity stores them, always in <see cref="UserRole"/> member order, since Identity promises none.
/// </summary>
internal static class AccountRoles
{
    private const string UpdatedAction = $"{nameof(AppUser)}.Updated";

    public static async Task<IReadOnlyList<UserRole>> RolesAsync(this UserManager<AppUser> users, AppUser user) =>
        [.. (await users.GetRolesAsync(user)).Select(Enum.Parse<UserRole>).Order()];

    /// <summary>
    /// The account's roles as the journal records them.
    /// </summary>
    public static async Task<string> JournalRoleAsync(this UserManager<AppUser> users, AppUser user) => JournalRole(await users.RolesAsync(user));

    public static string JournalRole(IEnumerable<UserRole> roles) => PlatformClaims.JournalRole(roles.Select(role => role.ToString()));

    /// <summary>
    /// Replaces the account's roles; a repeated role counts once. Identity saves the account once for the removed roles and once for the added ones,
    /// each save raising <see cref="AppUser.Version"/>; the change is journaled as an update of the account's <c>roles</c>, staged before the first save.
    /// Call it in the caller's open transaction, so the roles, the event and the caller's other changes commit together.
    /// </summary>
    /// <param name="users">The user manager.</param>
    /// <param name="user">The tracked account, already saved.</param>
    /// <param name="roles">The new roles.</param>
    /// <param name="audit">The journal writer.</param>
    /// <param name="context">The context the user manager saves through.</param>
    /// <returns>The first failed result, or success, also when the roles stay as they are.</returns>
    public static async Task<IdentityResult> SetRolesAsync(
        this UserManager<AppUser> users,
        AppUser user,
        IReadOnlyCollection<UserRole> roles,
        IAuditWriter audit,
        AppDbContext context)
    {
        var before = await users.RolesAsync(user);
        IReadOnlyList<UserRole> after = [.. roles.Distinct().Order()];
        var removed = before.Except(after).Select(role => role.ToString()).ToList();
        var added = after.Except(before).Select(role => role.ToString()).ToList();
        if (removed.Count == 0 && added.Count == 0)
        {
            return IdentityResult.Success;
        }

        audit.Stage(
            new AuditEntry(UpdatedAction, nameof(AppUser), user.Id.ToString(), OldValue: RolesJson(before), NewValue: RolesJson(after)),
            context);
        if (removed.Count > 0 && await users.RemoveFromRolesAsync(user, removed) is { Succeeded: false } failure)
        {
            return failure;
        }

        return added.Count > 0 ? await users.AddToRolesAsync(user, added) : IdentityResult.Success;
    }

    private static string RolesJson(IReadOnlyList<UserRole> roles) => JsonSerializer.Serialize(new { roles });
}
