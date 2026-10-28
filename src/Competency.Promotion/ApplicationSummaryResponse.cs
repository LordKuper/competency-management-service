namespace Competency.Promotion;

/// <summary>
/// An application as a row of a list.
/// </summary>
/// <param name="Id">The application.</param>
/// <param name="WaveId">The wave.</param>
/// <param name="WaveStartsOn">The first day of the first stage of the wave.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 to 6 while a stage runs, 7 once the last stage has ended.</param>
/// <param name="Status">Where the application stands, as the reader may know it.</param>
/// <param name="CandidateEmployeeId">The candidate.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Target">The level the application aims at.</param>
/// <param name="SubmittedAt">When the application was filed.</param>
/// <param name="Decision">The decision of the commission, as the reader may know it: a candidate learns it when the results are published.</param>
internal sealed record ApplicationSummaryResponse(
    Guid Id,
    Guid WaveId,
    DateOnly WaveStartsOn,
    int Stage,
    ApplicationStatus Status,
    Guid CandidateEmployeeId,
    string CandidateName,
    ApplicationTargetResponse Target,
    DateTimeOffset SubmittedAt,
    PromotionDecision? Decision);
