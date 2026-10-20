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
    /// A level has no required score.
    /// </summary>
    LevelWithoutRequirements,

    /// <summary>
    /// The block coefficients of a level do not add up to 100 %.
    /// </summary>
    BlockWeights,

    /// <summary>
    /// The version refers to an entry it may no longer use.
    /// </summary>
    InactiveEntry,

    /// <summary>
    /// A competency of a track matrix is not of the matrix direction.
    /// </summary>
    ForeignCompetency,

    /// <summary>
    /// A required score is not a point of the competency's own scale.
    /// </summary>
    ScoreOutOfScale,

    /// <summary>
    /// A level of the matrix track has no linked active position.
    /// </summary>
    LevelWithoutActivePosition,

    /// <summary>
    /// A position matrix version has no competencies.
    /// </summary>
    NoCompetencies,

    /// <summary>
    /// A competency of a position matrix version has no required score.
    /// </summary>
    CompetencyWithoutScore,

    /// <summary>
    /// A competency of a track matrix version has no required score at any level.
    /// </summary>
    CompetencyWithoutRequirements,

    /// <summary>
    /// A competency is in the version twice, in two of its groups.
    /// </summary>
    DuplicateCompetency,

    /// <summary>
    /// The position of a position matrix is no longer on the lists of the matrix unit and the units above it.
    /// </summary>
    PositionUnavailable,

    /// <summary>
    /// A shared block of the version applies to a unit the matrix unit is not in, nor below.
    /// </summary>
    SharedBlockUnavailable,
}
