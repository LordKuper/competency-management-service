namespace Kalibr.CareerFramework;

/// <summary>
/// The share of a group of competencies in the rating of a level, in percent: a shared block, or null for the competencies outside
/// blocks. A group's score is the average of its competencies' scores, plain or weighted by <see cref="MatrixCompetencyWeight"/>, each as a share
/// of its scale's highest score, so groups on different scales compare. A group has no threshold of its own.
/// </summary>
internal sealed record MatrixBlockWeight(Guid LevelId, Guid? BlockId, decimal Percent);
