namespace Kalibr.CareerFramework;

/// <summary>
/// How one competency stands against the score a level requires of it.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="RequiredScore">The score the level requires.</param>
/// <param name="Score">The score given; null when there is none.</param>
/// <param name="IsMet">Whether a score was given and it is not below the required one.</param>
public sealed record CareerCompetencyScore(Guid CompetencyId, int RequiredScore, decimal? Score, bool IsMet);
