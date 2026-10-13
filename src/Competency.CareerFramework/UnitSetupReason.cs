using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// Why a unit lacks one thing it needs or waits for it. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UnitSetupReason>))]
internal enum UnitSetupReason
{
    NoHead,
    NoAccount,
    Invited,
    Blocked,

    /// <summary>
    /// Neither the unit nor any unit above it has an HR assignment.
    /// </summary>
    NoHr,

    /// <summary>
    /// The assignment that applies names an account that cannot act, which leaves the unit without an HR.
    /// </summary>
    HrCannotAct,

    /// <summary>
    /// Neither the unit nor any unit above it lists a position.
    /// </summary>
    NoPositions,
}
