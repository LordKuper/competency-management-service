using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kalibr.Promotion;

/// <summary>
/// Runs <see cref="PromotionTick"/> by the host clock, which moves the waves from stage to stage: every minute, or every <c>Promotion:TickInterval</c>
/// when the configuration sets one. A failed pass is logged and the loop goes on. The first pass comes one interval after the start, and a service
/// that was down catches up then.
/// </summary>
internal sealed class PromotionScheduler(PromotionTick tick, TimeProvider time, IConfiguration configuration, ILogger<PromotionScheduler> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(configuration.GetValue("Promotion:TickInterval", DefaultInterval), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await tick.RunAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "A pass over the promotion waves failed; the next one runs on schedule.");
            }
        }
    }
}
