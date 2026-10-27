namespace Competency.Promotion;

/// <summary>
/// A candidate in the ranking of a commission. Only the commission of the unit and administrators see it.
/// </summary>
/// <param name="Rank">The position among the candidates with a rating, from 1; null for a candidate the chair has not scored.</param>
/// <param name="ApplicationId">The application.</param>
/// <param name="CandidateEmployeeId">The candidate.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Target">The level the application aims at.</param>
/// <param name="Status">Where the application stands.</param>
/// <param name="Rating">The rating the chair's scores give, in percent, unrounded; null until the chair gives scores.</param>
/// <param name="Threshold">The rating the target level requires, in percent; null when the version sets none.</param>
/// <param name="IsReady">Whether the chair's scores make the candidate ready by the rules of the version; null until the chair gives scores.</param>
/// <param name="Decision">The decision of the chair; null while there is none.</param>
internal sealed record RankingItemResponse(
    int? Rank,
    Guid ApplicationId,
    Guid CandidateEmployeeId,
    string CandidateName,
    ApplicationTargetResponse Target,
    ApplicationStatus Status,
    decimal? Rating,
    decimal? Threshold,
    bool? IsReady,
    PromotionDecision? Decision);
