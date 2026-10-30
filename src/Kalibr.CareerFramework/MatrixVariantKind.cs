using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// Who produced a variant of a matrix version. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatrixVariantKind>))]
internal enum MatrixVariantKind
{
    /// <summary>
    /// The content the unit head submitted.
    /// </summary>
    Author,

    /// <summary>
    /// The content an approver returned with changes.
    /// </summary>
    Approver,
}
