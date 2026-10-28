namespace Competency.Promotion;

/// <summary>
/// A candidate in the ranking of a commission. Only the commission of the unit and administrators see it.
/// </summary>
/// <param name="Rank">The position among the candidates by rating, from 1.</param>
/// <param name="ApplicationId">The application.</param>
/// <param name="CandidateEmployeeId">The candidate.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Target">The level the application aims at.</param>
/// <param name="Status">Where the application stands.</param>
/// <param name="Rating">The rating, in percent, unrounded: from the chair's saved final scores, and until there are any from the default ones, the raters' averages rounded.</param>
/// <param name="IsRatingFromFinalScores">Whether the rating comes from final scores the chair saved rather than from the defaults.</param>
/// <param name="Threshold">The rating the target level requires, in percent; null when the version sets none.</param>
/// <param name="IsReady">Whether the scores behind the rating make the candidate ready by the rules of the version.</param>
/// <param name="Decision">The decision of the chair; null while there is none.</param>
internal sealed record RankingItemResponse(
    int Rank,
    Guid ApplicationId,
    Guid CandidateEmployeeId,
    string CandidateName,
    ApplicationTargetResponse Target,
    ApplicationStatus Status,
    decimal Rating,
    bool IsRatingFromFinalScores,
    decimal? Threshold,
    bool IsReady,
    PromotionDecision? Decision);
