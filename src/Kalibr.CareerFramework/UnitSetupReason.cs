using System.Text.Json.Serialization;

namespace Kalibr.CareerFramework;

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
    /// No assignment applies and no employee could be assigned, since no account that can act and has the HR role is bound to one.
    /// </summary>
    NoHrCandidates,

    /// <summary>
    /// The assignment that applies names an employee whose account cannot act as an HR, which leaves the unit without an HR.
    /// </summary>
    HrCannotAct,

    /// <summary>
    /// The assigned HR's account can act but lacks the HR role.
    /// </summary>
    NoHrRole,

    /// <summary>
    /// Neither the unit nor any unit above it lists a position.
    /// </summary>
    NoPositions,
}
