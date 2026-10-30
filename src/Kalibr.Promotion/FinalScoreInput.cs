namespace Kalibr.Promotion;

/// <summary>
/// The score the chair gives one competency of a candidate.
/// </summary>
/// <param name="CompetencyId">The competency of the matrix version.</param>
/// <param name="Score">A point of the competency's scale.</param>
internal sealed record FinalScoreInput(Guid CompetencyId, int Score);
