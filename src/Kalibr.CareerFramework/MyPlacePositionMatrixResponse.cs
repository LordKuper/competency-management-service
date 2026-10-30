namespace Competency.CareerFramework;

/// <summary>
/// The published version of the position matrix of the user's unit for the user's position, with what its one column requires.
/// </summary>
/// <param name="MatrixId">The matrix.</param>
/// <param name="PositionName">The position's name as it stood at publication.</param>
/// <param name="VersionId">The published version.</param>
/// <param name="Number">The version number within the matrix.</param>
/// <param name="PublishedAt">When the version was published.</param>
/// <param name="Requirements">What the version requires of each competency, scores labelled by each competency's scale.</param>
internal sealed record MyPlacePositionMatrixResponse(
    Guid MatrixId,
    string PositionName,
    Guid VersionId,
    int Number,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<MyPlaceRequirementResponse> Requirements);
