using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// Which career catalogue an entry belongs to, for lists that mix entries of several catalogues.
/// Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CatalogEntryKind>))]
internal enum CatalogEntryKind
{
    Direction,
    Track,
    Competency,
    Scale,
    Block,
}
