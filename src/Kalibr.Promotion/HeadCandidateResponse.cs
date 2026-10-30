namespace Kalibr.Promotion;

/// <summary>
/// A candidate of a unit the signed-in head leads, with how complete the list of their raters is.
/// </summary>
/// <param name="ApplicationId">The application.</param>
/// <param name="CandidateEmployeeId">The candidate.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Target">The level the application aims at.</param>
/// <param name="Status">Where the application stands.</param>
/// <param name="CanEditRaters">Whether the head may change the list of raters now.</param>
/// <param name="RaterCounts">The raters listed against what is needed, with the shortfall and the addresses that wait for confirmation.</param>
internal sealed record HeadCandidateResponse(
    Guid ApplicationId,
    Guid CandidateEmployeeId,
    string CandidateName,
    ApplicationTargetResponse Target,
    ApplicationStatus Status,
    bool CanEditRaters,
    RaterCountsResponse RaterCounts);
