namespace Competency.Promotion;

/// <summary>
/// What a candidate needs to fill in an application for a matrix of an open wave.
/// </summary>
/// <param name="MatrixId">The matrix of the unit.</param>
/// <param name="UnitId">The unit whose matrix it is.</param>
/// <param name="UnitName">The unit name.</param>
/// <param name="TrackName">The track name as the bound version froze it.</param>
/// <param name="Levels">The levels the candidate can apply for, the most junior first.</param>
/// <param name="Competencies">The competencies the candidate scores.</param>
internal sealed record TargetFormResponse(
    Guid MatrixId,
    Guid UnitId,
    string UnitName,
    string TrackName,
    IReadOnlyList<TargetFormLevelResponse> Levels,
    IReadOnlyList<TargetFormCompetencyResponse> Competencies);
