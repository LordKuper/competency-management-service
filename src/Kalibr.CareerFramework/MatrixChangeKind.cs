using System.Text.Json.Serialization;

namespace Kalibr.CareerFramework;

/// <summary>
/// The kind of item a <see cref="MatrixChange"/> concerns. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatrixChangeKind>))]
internal enum MatrixChangeKind
{
    Level,
    Block,
    Competency,
    BlockWeight,
    CompetencyWeight,
    Requirement,
}
