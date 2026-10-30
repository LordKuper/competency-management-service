using System.Text.Json.Serialization;

namespace Kalibr.UserManagement;

/// <summary>
/// A role an account may hold; an account holds any set of them, and one without any is a plain user who may do only what the read policies allow.
/// Member names are issued as role claims and sent in the API, so renaming one changes both; the order of members is the order the API lists them in.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserRole>))]
internal enum UserRole
{
    /// <summary>
    /// Manages accounts, the org structure and everything else.
    /// </summary>
    GlobalAdmin,

    /// <summary>
    /// Reads the whole org structure and keeps the shared catalogues; it makes the account's employee a candidate for responsible HR of a unit.
    /// </summary>
    Hr,
}
