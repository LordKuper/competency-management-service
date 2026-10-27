namespace Competency.Promotion;

/// <summary>
/// The score the chair gave a competency.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="CompetencyName">The competency name as the version of that application froze it.</param>
/// <param name="Score">A point of the competency's scale.</param>
internal sealed record FinalScoreResponse(Guid CompetencyId, string CompetencyName, int Score);
