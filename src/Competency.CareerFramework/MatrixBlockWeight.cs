namespace Competency.CareerFramework;

/// <summary>
/// The share of a group of competencies in the rating of a level, in percent, and the group's minimum at that level, in percent of its
/// maximum. The group is an own block, a shared block, or null for the competencies outside blocks. Scores of different scales count
/// as shares of their scale's highest score, so groups on different scales compare.
/// </summary>
internal sealed record MatrixBlockWeight(Guid LevelId, Guid? BlockId, decimal Percent, decimal? MinPercent);
