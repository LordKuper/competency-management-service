using Kalibr.CareerFramework;

namespace Kalibr.Promotion;

/// <summary>
/// What the answers of the raters amount to: the one place that turns them into the averages the rules of the version are applied to, and into
/// the summary the commission sees. A competency every rater left as not observed has no average, so the rules count it as unmet.
/// </summary>
internal static class CircularRating
{
    /// <summary>
    /// Averages the scores given to each competency, leaving out those who did not observe it.
    /// </summary>
    /// <param name="answers">The answers of the raters of one application.</param>
    /// <returns>The mean score by competency; a competency nobody scored is absent.</returns>
    public static Dictionary<Guid, decimal> Averages(IEnumerable<PromotionRaterScore> answers) =>
        answers
            .Where(answer => answer.Score is not null)
            .GroupBy(answer => answer.CompetencyId)
            .ToDictionary(group => group.Key, group => group.Average(answer => (decimal)answer.Score!.Value));

    /// <summary>
    /// Turns the averages into the scores the chair starts from: each average rounded to a whole point, halves up.
    /// </summary>
    /// <param name="averages">The mean score by competency, as <see cref="Averages"/> returns it.</param>
    /// <returns>The default final score by competency; a competency nobody scored is absent.</returns>
    public static Dictionary<Guid, decimal> DefaultFinalScores(IReadOnlyDictionary<Guid, decimal> averages) =>
        averages.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, MidpointRounding.AwayFromZero));

    /// <summary>
    /// Finds the competencies the candidate must give an artifact for: those whose average does not meet the requirement.
    /// </summary>
    /// <param name="standing">The rules of the version applied to the averages.</param>
    /// <returns>The competencies that are not met.</returns>
    public static List<Guid> UnmetCompetencyIds(CareerScore standing) =>
        [.. standing.Competencies.Where(competency => !competency.IsMet).Select(competency => competency.CompetencyId)];

    /// <summary>
    /// Sums up the answers about one competency without a name or a single answer in it.
    /// </summary>
    /// <param name="competency">The competency.</param>
    /// <param name="answers">The answers of the raters about it.</param>
    /// <returns>The average, the number of raters per point of the scale and the number who did not observe it.</returns>
    public static CircularSummaryResponse Summarize(CareerVersionCompetency competency, IReadOnlyCollection<PromotionRaterScore> answers)
    {
        var scored = answers.Where(answer => answer.Score is not null).ToList();
        return new CircularSummaryResponse(
            scored.Count == 0 ? null : scored.Average(answer => (decimal)answer.Score!.Value),
            [.. competency.ScalePoints.Select(point => new ScoreCountResponse(point.Score, scored.Count(answer => answer.Score == point.Score)))],
            answers.Count - scored.Count);
    }
}
