namespace Competency.CareerFramework;

/// <summary>
/// The share of a block in the rating of a level, in percent, and the block's minimum score at that level, in percent of its maximum.
/// </summary>
internal sealed record MatrixBlockWeight(Guid LevelId, Guid BlockId, decimal Percent, decimal? MinPercent);
