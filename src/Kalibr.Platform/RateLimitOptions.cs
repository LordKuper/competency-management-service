namespace Competency.Platform;

/// <summary>
/// The request budget per client address of one rate-limiting policy, bound from its <c>RateLimiting</c> subsection.
/// </summary>
internal sealed class RateLimitOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}
