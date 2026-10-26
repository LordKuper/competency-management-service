namespace Competency.CareerFramework;

/// <summary>
/// What the desk needs of a matrix version in work or published, without its documents.
/// </summary>
/// <param name="Id">The version.</param>
/// <param name="MatrixId">The version's matrix.</param>
/// <param name="UnitId">The matrix unit.</param>
/// <param name="TrackId">The track of a track matrix; null for a position matrix.</param>
/// <param name="PositionId">The position of a position matrix; null for a track matrix.</param>
/// <param name="Number">The version number within the matrix.</param>
/// <param name="Status">The version's status.</param>
/// <param name="Step">The step the version waits at, or the step that returned it.</param>
/// <param name="AuthorId">The account that created the version.</param>
/// <param name="CreatedAt">When the version was created.</param>
/// <param name="BuilderStep">The matrix builder step the version was last saved at.</param>
internal sealed record CareerDeskVersion(
    Guid Id,
    Guid MatrixId,
    Guid UnitId,
    Guid? TrackId,
    Guid? PositionId,
    int Number,
    MatrixVersionStatus Status,
    ApprovalStep? Step,
    Guid AuthorId,
    DateTimeOffset CreatedAt,
    short? BuilderStep);
