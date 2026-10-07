using System.Text.Json.Serialization;

namespace Competency.UserManagement;

/// <summary>
/// What an account may do: global administrators manage everything, users only what the read policies allow them.
/// Member names are stored, issued as the role claim and sent in the API, so renaming one changes all three.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserRole>))]
internal enum UserRole
{
    User,
    GlobalAdmin,
}
