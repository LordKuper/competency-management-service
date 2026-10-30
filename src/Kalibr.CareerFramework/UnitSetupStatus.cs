using System.Text.Json.Serialization;

namespace Kalibr.CareerFramework;

/// <summary>
/// Whether a unit has one thing it needs. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UnitSetupStatus>))]
internal enum UnitSetupStatus
{
    Ready,
    Missing,

    /// <summary>
    /// The thing depends on another one the unit lacks, such as the head's account on the head.
    /// </summary>
    Waiting,
}
