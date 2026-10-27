namespace Competency.Promotion;

/// <summary>
/// A candidate a rater was asked to rate. The rater learns only the name.
/// </summary>
/// <param name="ApplicationId">The application.</param>
/// <param name="WaveId">The wave.</param>
/// <param name="CandidateName">The candidate's full name.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 to 7 while a stage runs, 8 once the last stage has ended.</param>
/// <param name="CanRate">Whether the rater may give or change their answers now.</param>
/// <param name="IsRated">Whether the rater has given their answers.</param>
internal sealed record RatingTaskResponse(Guid ApplicationId, Guid WaveId, string CandidateName, int Stage, bool CanRate, bool IsRated);
