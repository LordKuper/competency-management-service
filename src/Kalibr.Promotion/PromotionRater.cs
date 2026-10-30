using Kalibr.Platform;

namespace Kalibr.Promotion;

/// <summary>
/// An employee asked to rate a candidate. A rater who has no account is invited to the system at the address the candidate gave, once the head of
/// the unit of the target or the responsible HR has confirmed it.
/// </summary>
[Audited]
internal sealed class PromotionRater
{
    [Audited]
    public Guid ApplicationId { get; init; }

    [Audited]
    public Guid EmployeeId { get; init; }

    [Audited]
    public RaterSource Source { get; init; }

    /// <summary>
    /// The address for the invitation; null for a rater who has an account.
    /// </summary>
    [Audited]
    public string? Email { get; set; }

    /// <summary>
    /// The account of the head or the responsible HR who confirmed <see cref="Email"/>; null while it is not confirmed.
    /// </summary>
    [Audited]
    public Guid? EmailConfirmedBy { get; set; }

    [Audited]
    public DateTimeOffset? EmailConfirmedAt { get; set; }

    /// <summary>
    /// Whether <see cref="Email"/> is given and confirmed, so an invitation may go to it.
    /// </summary>
    public bool IsEmailConfirmed => Email is not null && EmailConfirmedBy is not null;
}
