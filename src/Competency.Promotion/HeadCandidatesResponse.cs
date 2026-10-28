namespace Competency.Promotion;

/// <summary>
/// The candidates of the units a head leads in a wave, the ones whose raters the head corrects.
/// </summary>
/// <param name="Session">The dates of the session.</param>
/// <param name="Candidates">The candidates by name.</param>
internal sealed record HeadCandidatesResponse(SessionDatesResponse Session, IReadOnlyList<HeadCandidateResponse> Candidates);
