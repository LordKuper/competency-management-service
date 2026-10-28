using Competency.CareerFramework;
using Competency.OrgStructure;

namespace Competency.Promotion;

/// <summary>
/// The card of a candidate for the commission: the application, what was learned about it at each stage, the chair's scores with the rating
/// and the readiness they give, the decision, and the candidate's earlier applications that got a decision. Only the commission of the unit and administrators see it.
/// </summary>
/// <param name="Id">The application.</param>
/// <param name="WaveId">The wave.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 to 6 while a stage runs, 7 once the last stage has ended.</param>
/// <param name="Status">Where the application stands.</param>
/// <param name="Roles">What the reader is to the application.</param>
/// <param name="CandidateEmployeeId">The candidate.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Target">The level the application aims at.</param>
/// <param name="Education">The candidate's own statement of their education.</param>
/// <param name="Formal">What the responsible HR entered for the formal check.</param>
/// <param name="FormalFailures">The conditions of the formal check the application did not meet.</param>
/// <param name="MeetsAfterRating">Whether the raters' averages meet the mandatory requirements; null until the rating stage has been checked.</param>
/// <param name="ArtifactsMissing">Whether the candidate gave no artifact for some competency that required one.</param>
/// <param name="Competencies">The competencies of the version in its order.</param>
/// <param name="Rating">The rating the chair's scores give, in percent, unrounded; null until the chair gives scores.</param>
/// <param name="Threshold">The rating the target level requires, in percent; null when the version sets none.</param>
/// <param name="IsReady">Whether the chair's scores make the candidate ready by the rules of the version; null until the chair gives scores.</param>
/// <param name="Decision">The decision of the chair; null while there is none.</param>
/// <param name="DecidedByName">Who recorded the decision, as people know the account.</param>
/// <param name="DecidedAt">When they recorded it.</param>
/// <param name="History">The candidate's earlier applications that got a decision and whose results were published, the latest first.</param>
internal sealed record ReviewResponse(
    Guid Id,
    Guid WaveId,
    int Stage,
    ApplicationStatus Status,
    IReadOnlyList<ApplicationRole> Roles,
    Guid CandidateEmployeeId,
    string CandidateName,
    ApplicationTargetResponse Target,
    EducationLevel Education,
    ApplicationFormalResponse Formal,
    IReadOnlyList<FormalFailure> FormalFailures,
    bool? MeetsAfterRating,
    bool ArtifactsMissing,
    IReadOnlyList<ReviewCompetencyResponse> Competencies,
    decimal? Rating,
    decimal? Threshold,
    bool? IsReady,
    PromotionDecision? Decision,
    string? DecidedByName,
    DateTimeOffset? DecidedAt,
    IReadOnlyList<ReviewHistoryResponse> History);
