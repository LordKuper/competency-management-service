using Kalibr.CareerFramework;

namespace Kalibr.Promotion;

/// <summary>
/// The one check of a set of scores against a matrix version, for self-scores, rater answers and final scores alike.
/// </summary>
internal static class ScoreRules
{
    /// <summary>
    /// Says why scores cannot stand for a version: a competency is scored twice, is not in the version, is missing (unless the scores are a draft), or has a score off its scale.
    /// </summary>
    /// <param name="version">The version the scores are for.</param>
    /// <param name="scores">The score given to each competency; null stands for "not observed".</param>
    /// <param name="allowNotObserved">Whether a competency may be left as not observed.</param>
    /// <param name="requireAll">Whether every competency must be scored; a draft may leave some out.</param>
    /// <returns>One line per problem, addressed to the user; empty when the scores are fine.</returns>
    public static List<string> Problems(CareerVersion version, IReadOnlyCollection<(Guid CompetencyId, int? Score)> scores, bool allowNotObserved, bool requireAll = true)
    {
        List<string> problems = [];
        var given = scores.GroupBy(score => score.CompetencyId).ToDictionary(group => group.Key, group => group.First().Score);
        if (given.Count != scores.Count)
        {
            problems.Add("Каждая компетенция оценивается один раз");
        }

        if (given.Keys.Any(id => version.Competencies.All(competency => competency.Id != id)))
        {
            problems.Add("Среди оценённых компетенций есть не входящая в матрицу");
        }

        foreach (var competency in version.Competencies.Where(competency => competency.ScalePoints.Count > 0))
        {
            if (!given.TryGetValue(competency.Id, out var score))
            {
                if (requireAll)
                {
                    problems.Add($"Оцените компетенцию «{competency.Name}»");
                }
            }
            else if (score is null ? !allowNotObserved : competency.ScalePoints.All(point => point.Score != score))
            {
                problems.Add($"Балл компетенции «{competency.Name}» должен быть баллом её шкалы");
            }
        }

        return problems;
    }
}
