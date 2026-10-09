namespace Competency.CareerFramework;

/// <summary>
/// One point of a scale: its score and what reaching it means.
/// </summary>
internal sealed record ScalePoint(int Score, string Label);
