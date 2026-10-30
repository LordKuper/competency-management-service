namespace Competency.Promotion;

/// <summary>
/// A critical competency that fell short in an earlier application.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="Name">The competency name as the version of that application froze it.</param>
/// <param name="RequiredScore">The score the target level required.</param>
/// <param name="Score">The average the raters gave; null when no rater observed the competency.</param>
internal sealed record ReviewFailedCompetencyResponse(Guid CompetencyId, string Name, int RequiredScore, decimal? Score);
