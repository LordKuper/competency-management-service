namespace Competency.Promotion;

/// <summary>
/// How many raters gave one point of a scale.
/// </summary>
/// <param name="Score">The point of the scale.</param>
/// <param name="Count">The number of raters who gave it.</param>
internal sealed record ScoreCountResponse(int Score, int Count);
