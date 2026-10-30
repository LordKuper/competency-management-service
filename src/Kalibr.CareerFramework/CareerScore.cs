namespace Kalibr.CareerFramework;

/// <summary>
/// The rating of a candidate for a level of a matrix version and whether it makes the candidate ready.
/// </summary>
/// <param name="Rating">The rating, in percent, unrounded.</param>
/// <param name="Threshold">The rating, in percent, the level requires; null when the version sets none.</param>
/// <param name="IsReady">Whether the rating reaches the threshold and every critical competency meets its required score.</param>
/// <param name="Competencies">Every competency with its required and given score.</param>
public sealed record CareerScore(decimal Rating, decimal? Threshold, bool IsReady, IReadOnlyList<CareerCompetencyScore> Competencies);
