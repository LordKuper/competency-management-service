using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// What keeps a matrix version from being handed on. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatrixProblemCode>))]
internal enum MatrixProblemCode
{
    /// <summary>
    /// The version has no levels.
    /// </summary>
    NoLevels,

    /// <summary>
    /// A level has no requirements.
    /// </summary>
    LevelWithoutRequirements,

    /// <summary>
    /// The block coefficients of a level do not add up to 100 %.
    /// </summary>
    BlockWeights,

    /// <summary>
    /// A critical competency has no minimum score.
    /// </summary>
    CriticalWithoutScore,

    /// <summary>
    /// The version refers to an entry it may no longer use.
    /// </summary>
    InactiveEntry,

    /// <summary>
    /// A competency is not on the version's scale or not of the matrix direction.
    /// </summary>
    ForeignCompetency,

    /// <summary>
    /// A required score is not a point of the version's scale.
    /// </summary>
    ScoreOutOfScale,

    /// <summary>
    /// A level of the matrix track has no linked active position.
    /// </summary>
    LevelWithoutActivePosition,
}
