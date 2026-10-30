using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Competency.Platform;

/// <summary>
/// Reports readiness by checking that the database accepts connections; exposes no failure details.
/// </summary>
internal sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
    }
}
