namespace Competency.Promotion;

/// <summary>
/// What the responsible HR enters for the formal check of an application.
/// </summary>
internal sealed record FormalRequest
{
    /// <summary>
    /// The date of hiring or of the last promotion.
    /// </summary>
    public required DateOnly HiredOrPromotedOn { get; init; }

    /// <summary>
    /// Whether the interval since that date is not to be held against the application.
    /// </summary>
    public required bool IgnoreInterval { get; init; }
}
