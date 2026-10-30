using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// How an item differs. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatrixChangeType>))]
internal enum MatrixChangeType
{
    Added,
    Removed,
    Changed,
}
