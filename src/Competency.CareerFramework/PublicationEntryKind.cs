using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// What kind of career entry a publication lists, as published with the root or in the way of it. Member names are sent in the API,
/// so renaming one changes it; the order is the order the list shows.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PublicationEntryKind>))]
internal enum PublicationEntryKind
{
    Direction,
    Track,
    Level,
    Competency,
    Scale,
    Block,
}
