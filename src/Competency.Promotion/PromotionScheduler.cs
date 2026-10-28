using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Competency.Promotion;

/// <summary>
/// Runs <see cref="PromotionTick"/> every minute by the host clock, which moves the waves from stage to stage. A failed pass is logged and the
/// loop goes on. The first pass comes one interval after the start, and a service that was down catches up then.
/// </summary>
internal sealed class PromotionScheduler(PromotionTick tick, TimeProvider time, ILogger<PromotionScheduler> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
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
