namespace Competency.Promotion;

/// <summary>
/// The score a candidate gives their own competency.
/// </summary>
/// <param name="CompetencyId">The competency of the matrix version.</param>
/// <param name="Score">A point of the competency's scale.</param>
internal sealed record SelfScoreInput(Guid CompetencyId, int Score);
