using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// Why an entry keeps a career entry from being active. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PublicationBlockerReason>))]
internal enum PublicationBlockerReason
{
    /// <summary>
    /// A draft the one whose right counts may not approve.
    /// </summary>
    NoRight,

    /// <summary>
    /// A track without levels.
    /// </summary>
    NoLevels,

    /// <summary>
    /// A level without a linked active position.
    /// </summary>
    NoActivePosition,

    /// <summary>
    /// An entry whose name another active entry, or another entry published with it, already has.
    /// </summary>
    NameTaken,
}
