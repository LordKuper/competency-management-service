using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// Which career catalogue an entry belongs to, for lists that mix entries of several catalogues.
/// Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CatalogEntryKind>))]
[Description("Category is deprecated and removed in the next major version: competencies have no category, so no entry is of that kind.")]
internal enum CatalogEntryKind
{
    Direction,
    Track,

    /// <summary>
    /// Kept for clients of the contract that had competency categories; no entry is of this kind.
    /// </summary>
    Category,

    Competency,
    Scale,
    Block,
}
