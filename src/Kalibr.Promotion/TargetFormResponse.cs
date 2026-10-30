using Kalibr.OrgStructure;

namespace Kalibr.Promotion;

/// <summary>
/// What a candidate needs to fill in an application for a matrix of an open wave.
/// </summary>
/// <param name="MatrixId">The matrix of the unit.</param>
/// <param name="UnitId">The unit whose matrix it is.</param>
/// <param name="UnitName">The unit name.</param>
/// <param name="TrackName">The track name as the bound version froze it.</param>
/// <param name="Education">The education on the candidate's employee card, which a new draft takes; null when the card has none.</param>
/// <param name="Levels">The levels the candidate may aim at, the most junior first: only the next level on the track of their position, any level on another track.</param>
/// <param name="Competencies">The competencies the candidate scores.</param>
internal sealed record TargetFormResponse(
    Guid MatrixId,
    Guid UnitId,
    string UnitName,
    string TrackName,
    EducationLevel? Education,
    IReadOnlyList<TargetFormLevelResponse> Levels,
    IReadOnlyList<TargetFormCompetencyResponse> Competencies);
