namespace Kalibr.Promotion;

/// <summary>
/// The address of a rater who has no account, entered or confirmed by the head of the unit of the target or by the responsible HR.
/// </summary>
internal sealed record RaterEmailRequest
{
    /// <summary>
    /// The address the invitation goes to; the one who sends it vouches for it.
    /// </summary>
    public required string Email { get; init; }
}
