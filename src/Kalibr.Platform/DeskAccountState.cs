using System.Text.Json.Serialization;

namespace Competency.Platform;

/// <summary>
/// Why an account a desk finding concerns cannot act. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeskAccountState>))]
public enum DeskAccountState
{
    /// <summary>
    /// The employee has no account.
    /// </summary>
    None,

    /// <summary>
    /// The account is invited and has no password yet.
    /// </summary>
    Invited,

    /// <summary>
    /// The account is blocked.
    /// </summary>
    Blocked,

    /// <summary>
    /// The account can act but lacks the HR role.
    /// </summary>
    NoHrRole,
}
