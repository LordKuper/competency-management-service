namespace Competency.Promotion;

/// <summary>
/// The rating the candidate's own scores would earn for the target level, computed by the rules of the version. A competency the candidate has not
/// scored counts as 0.
/// </summary>
/// <param name="Rating">The rating, in percent, unrounded.</param>
/// <param name="Threshold">The rating the target level requires, in percent; null when the version sets none.</param>
/// <param name="IsReady">Whether the rating reaches the threshold and every critical competency meets its required score.</param>
/// <param name="AreCriticalMet">Whether every critical competency meets its required score.</param>
/// <param name="UnscoredCount">The number of competencies that count towards the rating and have no score yet.</param>
internal sealed record PreliminaryRatingResponse(decimal Rating, decimal? Threshold, bool IsReady, bool AreCriticalMet, int UnscoredCount);
