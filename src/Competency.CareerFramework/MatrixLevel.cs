namespace Competency.CareerFramework;

/// <summary>
/// A career level of the version's track, with the overall readiness threshold, in percent of the rating, a candidate must reach.
/// </summary>
internal sealed record MatrixLevel(Guid LevelId, decimal? MinOverallPercent);
