using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// What kind of entry keeps a career entry from being active. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PublicationBlockerKind>))]
internal enum PublicationBlockerKind
{
    Track,
    Level,
}
