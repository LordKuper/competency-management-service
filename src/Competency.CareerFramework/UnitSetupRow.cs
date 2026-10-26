using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// One thing a unit needs to work, in the order a screen lists them. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UnitSetupRow>))]
internal enum UnitSetupRow
{
    Head,
    HeadAccount,
    Hr,
    HrAccount,
    Positions,
}
