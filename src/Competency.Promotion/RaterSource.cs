using System.Text.Json.Serialization;

namespace Competency.Promotion;

/// <summary>
/// Who put a rater on the list. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RaterSource>))]
internal enum RaterSource
{
    /// <summary>
    /// The candidate proposed the rater.
    /// </summary>
    Candidate,

    /// <summary>
    /// The head of the target unit added the rater.
    /// </summary>
    Head,
}
