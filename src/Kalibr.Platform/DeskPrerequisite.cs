using System.Text.Json.Serialization;

namespace Kalibr.Platform;

/// <summary>
/// A fact about the current data that a desk scenario needs before the user can start it.
/// Member names are sent in the API, so renaming one changes it; a new member also needs a source that knows it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeskPrerequisite>))]
public enum DeskPrerequisite
{
    /// <summary>
    /// The organization has an active unit.
    /// </summary>
    ActiveUnit,

    /// <summary>
    /// The position catalogue has an active position.
    /// </summary>
    ActivePosition,

    /// <summary>
    /// The organization has a working (not dismissed) employee.
    /// </summary>
    WorkingEmployee,

    /// <summary>
    /// The user may open at least one track.
    /// </summary>
    VisibleTrack,

    /// <summary>
    /// An account with the HR role is neither blocked nor still invited.
    /// </summary>
    HrAccount,

    /// <summary>
    /// The user may put at least one unit into a new promotion wave.
    /// </summary>
    WaveUnitAvailable,
}
