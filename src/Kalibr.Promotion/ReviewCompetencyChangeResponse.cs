namespace Kalibr.Promotion;

/// <summary>
/// How the average of the raters for a competency moved since the latest earlier application of the candidate that got a decision.
/// </summary>
/// <param name="IsNew">Whether the competency was not in that application.</param>
/// <param name="PreviousAverage">The average then; null for a new competency and when no rater observed it.</param>
/// <param name="Delta">The change in averages as shown, to one decimal place; null when either average is missing.</param>
internal sealed record ReviewCompetencyChangeResponse(bool IsNew, decimal? PreviousAverage, decimal? Delta);
