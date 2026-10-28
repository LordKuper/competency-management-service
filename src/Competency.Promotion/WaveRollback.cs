using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Takes a wave back to a stage that has passed. Everything people entered stays; what a later stage settled is reset, and the wave is marked as
/// not having carried out the end actions of the stage it returns to and the ones after it, so they run again when those stages end. A caller
/// holds the promotion locks first. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class WaveRollback(AppDbContext context, PromotionLocks locks, PromotionCalendar calendar)
{
    private const int FormalCheckStage = 2;
    private const int ArtifactStage = 4;
    private const int ReviewStage = 5;
    private const int ResultStage = 6;

    /// <summary>
    /// Finds what going back to a stage would reset or compute again.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="stage">The stage to go back to.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The consequences, with the number of applications each touches.</returns>
    public async Task<RollbackPreviewResponse> PreviewAsync(PromotionWave wave, int stage, CancellationToken cancellationToken)
    {
        var standing = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && application.Status != ApplicationStatus.Withdrawn)
            .Select(application => new { application.Status, application.MeetsAfterRating })
            .ToListAsync(cancellationToken);
        var effects = new List<RollbackEffect>();
        if (stage <= FormalCheckStage)
        {
            effects.Add(new RollbackEffect(
                RollbackEffectKind.FormalAdmittedReset,
                standing.Count(application => application.Status is not (ApplicationStatus.Submitted or ApplicationStatus.FormalRejected))));
            effects.Add(new RollbackEffect(RollbackEffectKind.FormalRejectedReset, standing.Count(application => application.Status == ApplicationStatus.FormalRejected)));
        }

        if (stage <= ReviewStage)
        {
            effects.Add(new RollbackEffect(RollbackEffectKind.NoDecisionCleared, standing.Count(application => application.Status == ApplicationStatus.NoDecision)));
        }

        if (stage <= DueStep.RatingStage)
        {
            effects.Add(new RollbackEffect(RollbackEffectKind.RatingRecalculated, standing.Count(application => application.MeetsAfterRating is not null)));
        }

        if (stage <= ArtifactStage)
        {
            effects.Add(new RollbackEffect(RollbackEffectKind.ArtifactsRecalculated, standing.Count(application => PromotionApplication.ReviewedStatuses.Contains(application.Status))));
        }

        if (stage < ResultStage && calendar.StageOf(wave) >= ResultStage)
        {
            effects.Add(new RollbackEffect(
                RollbackEffectKind.ResultsHidden,
                standing.Count(application => application.Status is ApplicationStatus.Decided or ApplicationStatus.NoDecision)));
        }

        return new RollbackPreviewResponse(
            stage,
            [.. effects.Where(effect => effect.Count > 0)],
            [.. WaveStep.StagesWithEndLetters.Where(letterStage => letterStage >= stage && letterStage <= wave.ProcessedStage)],
            stage <= DueStep.RatingStage && wave.RaterReminderSent);
    }

    /// <summary>
    /// Takes the wave and its applications back to a stage, changing tracked rows and saving nothing, so the caller commits them in its
    /// transaction. The wave must be locked.
    /// </summary>
    /// <param name="wave">The locked wave.</param>
    /// <param name="request">The stage and the new dates, already checked.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task ApplyAsync(PromotionWave wave, RollbackRequest request, CancellationToken cancellationToken)
    {
        foreach (var application in await locks.FindWaveApplicationsForUpdateAsync(wave.Id, cancellationToken))
        {
            application.ReturnToStage(request.Stage);
        }

        wave.EndsOn = [.. request.EndsOn];
        wave.ProcessedStage = Math.Min(wave.ProcessedStage, request.Stage - 1);
        if (request.Stage <= DueStep.RatingStage)
        {
            wave.RaterReminderSent = false;
        }
    }
}
