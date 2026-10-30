using Kalibr.CareerFramework;
using Kalibr.OrgStructure;

namespace Kalibr.Promotion;

/// <summary>
/// An application as its reader may see it. The candidate sees their own application and the raters they proposed; the result only from the
/// stage that publishes it. What belongs to other roles is null for everybody else.
/// </summary>
/// <param name="Id">The application.</param>
/// <param name="WaveId">The wave.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 to 6 while a stage runs, 7 once the last stage has ended.</param>
/// <param name="Session">The dates of the session, for every role.</param>
/// <param name="Status">Where the application stands.</param>
/// <param name="Roles">What the reader is to the application.</param>
/// <param name="CandidateEmployeeId">The candidate.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Target">The level the application aims at.</param>
/// <param name="Education">The candidate's own statement of their education.</param>
/// <param name="SelfScores">The candidate's scores by competency, in the order of the version, each with what the form and the artifact table show of the competency.</param>
/// <param name="PreliminaryRating">The rating the candidate's scores earn so far, computed by the server whenever the scores change; null for those other than the candidate.</param>
/// <param name="Raters">The raters; null for those who do not see them.</param>
/// <param name="RaterCounts">The raters listed against what is needed; null for those who do not see the raters.</param>
/// <param name="Formal">What the responsible HR entered; null for those who do not see it.</param>
/// <param name="FormalFailures">The conditions of the formal check the application did not meet, once the stage that checks it has ended.</param>
/// <param name="Warnings">The conditions that stand against the application now, while it waits for the formal check; the reader sees those that concern them.</param>
/// <param name="FormalCheck">What the formal check would decide now, while the application waits for it; null for those who do not see the formal data.</param>
/// <param name="Artifacts">The artifacts the candidate gave, which may be given in a draft and in the artifact stage; null for those who do not see them.</param>
/// <param name="MandatoryArtifactCompetencyIds">The competencies the candidate must give an artifact for, once the rating stage has been checked; null for those who do not see them.</param>
/// <param name="MissingArtifactCompetencyIds">The competencies among the mandatory ones that still have no artifact, which the candidate is warned of before sending the application; null for those who do not see them.</param>
/// <param name="IsSentToReview">Whether the candidate sent the application to the commission themselves, after which the artifacts are read-only.</param>
/// <param name="HasMandatoryArtifacts">Whether the candidate owes artifacts, so the explanation that a competency without one is probably not confirmed is shown beside them; false for those who do not see them.</param>
/// <param name="FeedbackReceivedAt">When the candidate marked the feedback as received; null until they do, and for those who do not see it.</param>
/// <param name="Result">The final scores against the requirements and the decision, for the candidate from the stage that publishes them; null otherwise.</param>
internal sealed record ApplicationResponse(
    Guid Id,
    Guid WaveId,
    int Stage,
    SessionDatesResponse Session,
    ApplicationStatus Status,
    IReadOnlyList<ApplicationRole> Roles,
    Guid CandidateEmployeeId,
    string CandidateName,
    ApplicationTargetResponse Target,
    EducationLevel Education,
    IReadOnlyList<SelfScoreResponse> SelfScores,
    PreliminaryRatingResponse? PreliminaryRating,
    IReadOnlyList<RaterResponse>? Raters,
    RaterCountsResponse? RaterCounts,
    ApplicationFormalResponse? Formal,
    IReadOnlyList<FormalFailure> FormalFailures,
    IReadOnlyList<FormalFailure> Warnings,
    FormalCheckResponse? FormalCheck,
    IReadOnlyList<ArtifactResponse>? Artifacts,
    IReadOnlyList<Guid>? MandatoryArtifactCompetencyIds,
    IReadOnlyList<Guid>? MissingArtifactCompetencyIds,
    bool IsSentToReview,
    bool HasMandatoryArtifacts,
    DateTimeOffset? FeedbackReceivedAt,
    ApplicationResultResponse? Result);
