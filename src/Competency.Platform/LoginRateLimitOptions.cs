namespace Competency.Platform;

/// <summary>
/// The sign-in attempt budget per client address, bound from <c>RateLimiting:Login</c>.
/// </summary>
internal sealed class LoginRateLimitOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}
