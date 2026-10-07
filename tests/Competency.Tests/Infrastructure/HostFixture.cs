using Xunit;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// A host with a database of its own for one test class, started with the given configuration overrides and stopped after the class.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
/// <param name="settings">Configuration overrides for the host, or null for the defaults of the shared host.</param>
public abstract class HostFixture(TestEnvironment environment, IReadOnlyDictionary<string, string>? settings) : IAsyncLifetime
{
    public ApiHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync() => Host = await environment.StartHostAsync(settings);

    public async ValueTask DisposeAsync() => await Host.DisposeAsync();
}

/// <summary>
/// A host with the production sign-in rate limit reduced to a few attempts, so that the limit can be reached in a test.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class StrictRateLimitHost(TestEnvironment environment)
    : HostFixture(environment, new Dictionary<string, string> { ["RateLimiting__Login__PermitLimit"] = StrictRateLimitHost.PermitLimit.ToString() })
{
    public const int PermitLimit = 3;
}
