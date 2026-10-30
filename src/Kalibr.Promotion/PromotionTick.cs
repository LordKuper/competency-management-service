using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Competency.Promotion;

/// <summary>
/// One pass over the unfinished waves: each is brought up to date, one step at a time in order, so what a stopped service missed is caught up
/// exactly once. Every step runs in its own scope and transaction, and the settings are read only when a pass runs, because the host builds this
/// service before they are validated. Safe for concurrent use: two passes meet at the row lock of a wave, and the one that
/// finds it taken leaves it to the other.
/// </summary>
internal sealed class PromotionTick(IServiceScopeFactory scopeFactory, ILogger<PromotionTick> logger)
{
    /// <summary>
    /// Brings every wave that is owed a step up to date. A wave that fails is logged and left for the next pass, without stopping the others.
    /// </summary>
    /// <param name="cancellationToken">Cancels the pass.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var waveId in await DueWaveIdsAsync(cancellationToken))
        {
            try
            {
                StepResult result;
                do
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    result = await scope.ServiceProvider.GetRequiredService<WaveStep>().RunAsync(waveId, cancellationToken);
                }
                while (result == StepResult.Done);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(exception, "A step of wave {WaveId} failed; it is tried again on the next pass.", waveId);
            }
        }
    }

    private async Task<List<Guid>> DueWaveIdsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var calendar = scope.ServiceProvider.GetRequiredService<PromotionCalendar>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var waves = await context.Set<PromotionWave>().AsNoTracking().Where(wave => wave.ProcessedStage < PromotionWave.StageCount).ToListAsync(cancellationToken);
        var withArrivals = (await context.Set<PromotionApplication>()
                .AsNoTracking()
                .Where(application => application.ReviewLetterPending)
                .Select(application => application.WaveId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var admitted = (await context.Set<PromotionApplication>()
                .AsNoTracking()
                .Where(application => application.Status == ApplicationStatus.Admitted && application.MeetsAfterRating != null)
                .Select(application => new { application.WaveId, Meets = application.MeetsAfterRating == true })
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToLookup(application => application.WaveId, application => application.Meets);
        return [.. waves.Where(wave => DueStep.Next(wave, calendar, withArrivals.Contains(wave.Id), CanMove(admitted[wave.Id], wave)) is not null).Select(wave => wave.Id)];
    }

    private static bool CanMove(IEnumerable<bool> admittedMeet, PromotionWave wave) =>
        admittedMeet.Any(meets => meets || wave.ProcessedStage >= PromotionStage.Artifacts);
}
