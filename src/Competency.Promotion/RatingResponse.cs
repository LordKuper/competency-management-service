namespace Competency.Promotion;

/// <summary>
/// What a rater sees of a candidate: the name and the competencies to answer about, with the rater's own answers if they gave any.
/// </summary>
/// <param name="ApplicationId">The application.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 to 7 while a stage runs, 8 once the last stage has ended.</param>
/// <param name="CanEdit">Whether the rater may give or change their answers now.</param>
/// <param name="Competencies">The competencies of the matrix version, in its order.</param>
/// <param name="Answers">The rater's own answers; null until they give them.</param>
internal sealed record RatingResponse(
    Guid ApplicationId,
    string CandidateName,
    int Stage,
    bool CanEdit,
    IReadOnlyList<TargetFormCompetencyResponse> Competencies,
    IReadOnlyList<RatingAnswerInput>? Answers);
