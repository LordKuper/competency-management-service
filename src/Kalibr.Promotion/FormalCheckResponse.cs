namespace Kalibr.Promotion;

/// <summary>
/// The result the formal check would have now, for the responsible HR to see while entering the dates; it is settled when the check stage ends.
/// </summary>
/// <param name="IsPassed">Whether the application would pass.</param>
/// <param name="Failures">The conditions it would fail, in a fixed order; empty when it passes.</param>
internal sealed record FormalCheckResponse(bool IsPassed, IReadOnlyList<FormalFailure> Failures);
