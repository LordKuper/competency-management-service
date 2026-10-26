using System.Text.Json.Serialization;

namespace Competency.Platform;

/// <summary>
/// The kinds of records desk findings are about or name. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeskObjectType>))]
public enum DeskObjectType
{
    /// <summary>
    /// A unit's competency matrix, with its versions.
    /// </summary>
    Matrix,

    /// <summary>
    /// One version of a competency matrix.
    /// </summary>
    MatrixVersion,

    /// <summary>
    /// An organization unit.
    /// </summary>
    OrgUnit,

    /// <summary>
    /// A career track.
    /// </summary>
    Track,

    /// <summary>
    /// A career level of a track.
    /// </summary>
    Level,

    /// <summary>
    /// A career direction.
    /// </summary>
    Direction,

    /// <summary>
    /// A competency.
    /// </summary>
    Competency,

    /// <summary>
    /// A competency scale.
    /// </summary>
    Scale,

    /// <summary>
    /// A shared block of competencies.
    /// </summary>
    Block,

    /// <summary>
    /// A position from the position catalogue.
    /// </summary>
    Position,

    /// <summary>
    /// An employee.
    /// </summary>
    Employee,

    /// <summary>
    /// A user account.
    /// </summary>
    UserAccount,
}
