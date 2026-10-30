namespace Competency.Promotion;

/// <summary>
/// A rater's answer about one competency.
/// </summary>
/// <param name="CompetencyId">The competency of the matrix version.</param>
/// <param name="Score">A point of the competency's scale; null when the rater did not observe the competency.</param>
internal sealed record RatingAnswerInput(Guid CompetencyId, int? Score);
