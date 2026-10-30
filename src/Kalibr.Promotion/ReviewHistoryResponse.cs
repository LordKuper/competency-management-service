namespace Kalibr.Promotion;

/// <summary>
/// An earlier application of a candidate that got a decision and whose results were published, as the commission sees it. Readiness and the critical
/// competencies are judged by the averages of the raters of that application, the same as when it was checked for the artifact stage.
/// </summary>
/// <param name="WaveId">The wave.</param>
/// <param name="WaveStartsOn">The first day of the first stage of the wave.</param>
/// <param name="Target">The track and level the application aimed at.</param>
/// <param name="Decision">The decision of the chair.</param>
/// <param name="Rating">The rating the raters' averages earned, in percent, unrounded.</param>
/// <param name="Threshold">The rating the target level required, in percent; null when the version set none.</param>
/// <param name="FailedCritical">The critical competencies whose average was below the required score; empty when all were met.</param>
internal sealed record ReviewHistoryResponse(
    Guid WaveId,
    DateOnly WaveStartsOn,
    ApplicationTargetResponse Target,
    PromotionDecision Decision,
    decimal Rating,
    decimal? Threshold,
    IReadOnlyList<ReviewFailedCompetencyResponse> FailedCritical);
