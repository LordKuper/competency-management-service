using System.Text.Json.Serialization;

namespace Competency.Platform;

/// <summary>
/// Why an invitation link no longer works or soon will not. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeskLinkState>))]
public enum DeskLinkState
{
    /// <summary>
    /// The link's time ran out.
    /// </summary>
    Expired,

    /// <summary>
    /// The link still works, but not for long.
    /// </summary>
    ExpiringSoon,

    /// <summary>
    /// The link was voided before its time ran out: by a change that ends the account's sessions, or by a new e-mail address.
    /// </summary>
    Revoked,
}
