using System.Text.Json.Serialization;

namespace Kalibr.Promotion;

/// <summary>
/// What a rater exclusion names. Member names are stored, so renaming one changes the data.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RaterExclusionKind>))]
internal enum RaterExclusionKind
{
    /// <summary>
    /// One employee.
    /// </summary>
    Employee,

    /// <summary>
    /// Everyone who holds a catalogue position now.
    /// </summary>
    Position,
}
