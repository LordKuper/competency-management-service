namespace Competency.CareerFramework;

/// <summary>
/// The weight, in percent, of a competency in the score of its group on a level, set for a group that has its own weights. The weights
/// of a group on a level add up to 100 %; a group without any on a level weighs its competencies equally.
/// </summary>
internal sealed record MatrixCompetencyWeight(Guid LevelId, Guid CompetencyId, decimal Percent);
