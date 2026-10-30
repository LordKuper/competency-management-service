using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// What kind of record keeps a career entry from being deleted. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeletionBlockerKind>))]
internal enum DeletionBlockerKind
{
    Track,
    Competency,

    /// <summary>
    /// A competency whose history, but not its current version, refers to the entry.
    /// </summary>
    CompetencyVersion,

    Matrix,

    /// <summary>
    /// A shared block whose composition holds the entry.
    /// </summary>
    Block,
}
