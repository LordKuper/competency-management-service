namespace Kalibr.CareerFramework;

/// <summary>
/// The share of a group of competencies in the rating of a level of a matrix version.
/// </summary>
/// <param name="LevelId">The level.</param>
/// <param name="BlockId">The shared block; null for the competencies outside blocks.</param>
/// <param name="Percent">The coefficient of the group, in percent.</param>
public sealed record CareerVersionBlockWeight(Guid LevelId, Guid? BlockId, decimal Percent);
