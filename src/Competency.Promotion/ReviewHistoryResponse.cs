namespace Competency.Promotion;

/// <summary>
/// An earlier application of a candidate whose results were published, as the commission sees it.
/// </summary>
/// <param name="WaveId">The wave.</param>
/// <param name="WaveStartsOn">The first day of the first stage of the wave.</param>
/// <param name="Target">The level the application aimed at.</param>
/// <param name="Decision">The decision of the chair; null when the commission stage ended with none.</param>
/// <param name="FinalScores">The scores the chair gave.</param>
internal sealed record ReviewHistoryResponse(
    Guid WaveId,
    DateOnly WaveStartsOn,
    ApplicationTargetResponse Target,
    PromotionDecision? Decision,
    IReadOnlyList<FinalScoreResponse> FinalScores);
