using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// How a competency counts towards readiness. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CompetencyType>))]
internal enum CompetencyType
{
    /// <summary>
    /// Must reach its minimum level; no other score makes up for a gap.
    /// </summary>
    Critical,

    /// <summary>
    /// Counts towards the overall rating as a core skill of the track.
    /// </summary>
    Profile,

    /// <summary>
    /// Counts towards the overall rating as a supplementary skill.
    /// </summary>
    Additional,
}
