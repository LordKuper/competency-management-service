using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.Promotion;

/// <summary>
/// Takes a wave back to a stage that has passed. Everything people entered stays; what a later stage settled is reset, and the wave is marked as
/// not having carried out the end actions of the stage it returns to and the ones after it, so they run again when those stages end. A caller
/// holds the promotion locks first. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class WaveRollback(AppDbContext context, PromotionLocks locks, PromotionCalendar calendar)
{
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
            .Where(application => application.WaveId == wave.Id && application.Status != ApplicationStatus.Withdrawn && application.Status != ApplicationStatus.Draft)
            .Select(application => new { application.Status, application.MeetsAfterRating, application.IsSentToReview })
            .ToListAsync(cancellationToken);
        var effects = new List<RollbackEffect>();
        if (stage <= PromotionStage.FormalCheck)
        {
            effects.Add(new RollbackEffect(
                RollbackEffectKind.FormalAdmittedReset,
                standing.Count(application => application.Status is not (ApplicationStatus.Submitted or ApplicationStatus.FormalRejected))));
            effects.Add(new RollbackEffect(RollbackEffectKind.FormalRejectedReset, standing.Count(application => application.Status == ApplicationStatus.FormalRejected)));
        }

        if (stage <= PromotionStage.Review)
        {
            effects.Add(new RollbackEffect(RollbackEffectKind.NoDecisionCleared, standing.Count(application => application.Status == ApplicationStatus.NoDecision)));
        }

        if (stage <= PromotionStage.Rating)
        {
            effects.Add(new RollbackEffect(RollbackEffectKind.RatingRecalculated, standing.Count(application => application.MeetsAfterRating is not null)));
        }

        if (stage <= PromotionStage.Artifacts)
        {
            effects.Add(new RollbackEffect(
                RollbackEffectKind.ArtifactsRecalculated,
                standing.Count(application => PromotionApplication.ReviewedStatuses.Contains(application.Status)
                    && (stage < PromotionStage.Artifacts || (application.MeetsAfterRating != true && !application.IsSentToReview)))));
        }

        if (stage < PromotionStage.Feedback && calendar.StageOf(wave) >= PromotionStage.Feedback)
        {
            effects.Add(new RollbackEffect(
                RollbackEffectKind.ResultsHidden,
                standing.Count(application => application.Status is ApplicationStatus.Decided or ApplicationStatus.NoDecision)));
        }

        return new RollbackPreviewResponse(
            stage,
            [.. effects.Where(effect => effect.Count > 0)],
            [.. WaveStep.StagesWithEndLetters.Where(letterStage => letterStage >= stage && letterStage <= wave.ProcessedStage)],
            stage <= PromotionStage.Rating && wave.RaterReminderSent);
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
        var applications = await locks.FindWaveApplicationsForUpdateAsync(wave.Id, cancellationToken);
        foreach (var application in applications)
        {
            application.ReturnToStage(request.Stage);
        }

        wave.EndsOn = [.. request.EndsOn];
        wave.ProcessedStage = Math.Min(wave.ProcessedStage, request.Stage - 1);
        if (request.Stage <= PromotionStage.Rating)
        {
            wave.RaterReminderSent = false;
        }
    }
}
