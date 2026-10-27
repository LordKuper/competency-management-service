using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// An application as its reader may see it. The candidate sees their own application and the raters they proposed; the result only from the
/// stage that publishes it. What belongs to other roles is null for everybody else.
/// </summary>
/// <param name="Id">The application.</param>
/// <param name="WaveId">The wave.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 to 7 while a stage runs, 8 once the last stage has ended.</param>
/// <param name="Status">Where the application stands.</param>
/// <param name="Roles">What the reader is to the application.</param>
/// <param name="CandidateEmployeeId">The candidate.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Target">The level the application aims at.</param>
/// <param name="Education">The candidate's own statement of their education.</param>
/// <param name="SelfScores">The candidate's scores by competency, in the order of the version.</param>
/// <param name="Raters">The raters; null for those who do not see them.</param>
/// <param name="Formal">What the responsible HR entered; null for those who do not see it.</param>
/// <param name="FormalFailures">The conditions of the formal check the application did not meet, once the stage that checks it has ended.</param>
/// <param name="Warnings">The conditions that stand against the application now, while it waits for the formal check; the reader sees those that concern them.</param>
internal sealed record ApplicationResponse(
    Guid Id,
    Guid WaveId,
    int Stage,
    ApplicationStatus Status,
    IReadOnlyList<ApplicationRole> Roles,
    Guid CandidateEmployeeId,
    string CandidateName,
    ApplicationTargetResponse Target,
    EducationLevel Education,
    IReadOnlyList<SelfScoreResponse> SelfScores,
    IReadOnlyList<RaterResponse>? Raters,
    ApplicationFormalResponse? Formal,
    IReadOnlyList<FormalFailure> FormalFailures,
    IReadOnlyList<FormalFailure> Warnings);
