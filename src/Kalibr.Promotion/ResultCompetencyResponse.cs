namespace Kalibr.Promotion;

/// <summary>
/// How the final score of one competency stands against what the target level requires.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="CompetencyName">The competency name as the bound version froze it.</param>
/// <param name="RequiredScore">The score the target level requires.</param>
/// <param name="Score">The final score; null when the chair gave none.</param>
/// <param name="IsMet">Whether a score was given and it is not below the required one.</param>
internal sealed record ResultCompetencyResponse(Guid CompetencyId, string CompetencyName, int RequiredScore, int? Score, bool IsMet);
