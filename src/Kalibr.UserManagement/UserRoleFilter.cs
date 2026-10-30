using System.Text.Json.Serialization;

namespace Kalibr.UserManagement;

/// <summary>
/// The role filter of the account list. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserRoleFilter>))]
internal enum UserRoleFilter
{
    /// <summary>
    /// Accounts without any role.
    /// </summary>
    User,

    /// <summary>
    /// Accounts with the global administrator role, whatever their other roles.
    /// </summary>
    GlobalAdmin,

    /// <summary>
    /// Accounts with the HR role, whatever their other roles.
    /// </summary>
    Hr,
}
