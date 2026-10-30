using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// Finds the critical competencies a rating leaves unmet: the one place that reads the type of a competency off the version.
/// </summary>
internal static class CriticalGaps
{
    /// <summary>
    /// Lists the critical competencies whose score is below the required one or missing.
    /// </summary>
    /// <param name="version">The version the rating was computed by.</param>
    /// <param name="standing">The rating.</param>
    /// <returns>The standing of each unmet critical competency, in the order of the version.</returns>
    public static List<CareerCompetencyScore> Of(CareerVersion version, CareerScore standing) =>
        [.. standing.Competencies.Where(competency => !competency.IsMet
            && version.Competencies.Any(candidate => candidate.Id == competency.CompetencyId && candidate.Type == CompetencyType.Critical))];
}
