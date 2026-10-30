using System.Text.Json.Serialization;

namespace Kalibr.CareerFramework;

/// <summary>
/// Whether the account of a unit's head or HR can act, and if not, why. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UnitAccountState>))]
internal enum UnitAccountState
{
    /// <summary>
    /// The person has no account.
    /// </summary>
    None,

    /// <summary>
    /// The account is invited and has no password yet.
    /// </summary>
    Invited,

    /// <summary>
    /// An administrator blocked the account.
    /// </summary>
    Blocked,

    /// <summary>
    /// The account of an assigned HR can act but lacks the HR role, so the assignment does not act.
    /// </summary>
    NoHrRole,

    /// <summary>
    /// The account can act.
    /// </summary>
    Active,
}
