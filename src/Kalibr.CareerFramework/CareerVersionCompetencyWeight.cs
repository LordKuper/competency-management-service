namespace Competency.CareerFramework;

/// <summary>
/// The weight of a competency in the score of its group on a level of a matrix version, set for a group with its own weights.
/// </summary>
/// <param name="LevelId">The level.</param>
/// <param name="CompetencyId">The competency.</param>
/// <param name="Percent">The weight of the competency in its group, in percent.</param>
public sealed record CareerVersionCompetencyWeight(Guid LevelId, Guid CompetencyId, decimal Percent);
