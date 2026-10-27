namespace Competency.CareerFramework;

/// <summary>
/// A requirements column of the version with the overall readiness threshold, in percent of the rating, a candidate must reach and the
/// education the column requires, if any: a career level of the matrix track, or the one column of a position matrix, keyed by its
/// position. A document stored without the education reads as no requirement.
/// </summary>
internal sealed record MatrixLevel(Guid LevelId, decimal? MinOverallPercent, EducationLevel? RequiredEducation = null);
