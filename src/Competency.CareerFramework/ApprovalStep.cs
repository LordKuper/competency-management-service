using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// A step of the approval route of a matrix version. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApprovalStep>))]
internal enum ApprovalStep
{
    /// <summary>
    /// The head of the nearest unit above the matrix unit whose head can act.
    /// </summary>
    Superior,

    /// <summary>
    /// The responsible HR of the matrix unit, whose approval publishes the version.
    /// </summary>
    Hr,
}
