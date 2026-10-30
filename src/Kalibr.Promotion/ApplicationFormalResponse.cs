namespace Kalibr.Promotion;

/// <summary>
/// What the responsible HR entered for the formal check.
/// </summary>
/// <param name="HiredOrPromotedOn">The date of hiring or of the last promotion; null until entered.</param>
/// <param name="IgnoreInterval">Whether the interval since that date is not held against the application.</param>
/// <param name="IgnoreIntervalByName">Who set that, as people know the account; null when nobody did.</param>
/// <param name="IgnoreIntervalAt">When they set it; null when nobody did.</param>
internal sealed record ApplicationFormalResponse(DateOnly? HiredOrPromotedOn, bool IgnoreInterval, string? IgnoreIntervalByName, DateTimeOffset? IgnoreIntervalAt);
