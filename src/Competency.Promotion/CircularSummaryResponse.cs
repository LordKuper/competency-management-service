namespace Competency.Promotion;

/// <summary>
/// The answers of all raters about one competency, with no name or single answer in it.
/// </summary>
/// <param name="Average">The mean of the scores given; null when every rater did not observe the competency.</param>
/// <param name="Distribution">The number of raters per point of the scale, every point of it.</param>
/// <param name="NotObservedCount">The number of raters who did not observe the competency.</param>
internal sealed record CircularSummaryResponse(decimal? Average, IReadOnlyList<ScoreCountResponse> Distribution, int NotObservedCount);
