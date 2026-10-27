namespace Competency.Promotion;

/// <summary>
/// The score a candidate gave a competency.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="CompetencyName">The competency name as the bound version froze it.</param>
/// <param name="Score">A point of the competency's scale.</param>
internal sealed record SelfScoreResponse(Guid CompetencyId, string CompetencyName, int Score);
