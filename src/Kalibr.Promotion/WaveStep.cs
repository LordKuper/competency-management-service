using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kalibr.Promotion;

/// <summary>
/// How an attempt to carry out the next step of a wave ended.
/// </summary>
internal enum StepResult
{
    /// <summary>
    /// Nothing was due or nothing could be done yet, or another transaction was changing the wave; nothing was committed.
    /// </summary>
    Idle,

    /// <summary>
    /// A step was carried out and committed.
    /// </summary>
    Done,
}

/// <summary>
/// Carries out one step of a wave in one transaction: the changes and the stage marker commit together, and the letters are composed after the commit.
/// Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class WaveStep(
    AppDbContext context,
    PromotionLocks locks,
    PromotionCalendar calendar,
    StageActions actions,
    PromotionMail mail,
    ILogger<WaveStep> logger)
{
    /// <summary>
    /// The stages whose end sends letters.
    /// </summary>
    public static readonly IReadOnlyList<int> StagesWithEndLetters = [1, 2, 3, 4, 5, 6];

    /// <summary>
    /// Carries out the next step a wave is owed. Applications reach a commission only if it has a chair who acts: those of a unit without one stay admitted
    /// while the stages end for the other units, and are handed over by a later call once the unit has a chair.
    /// </summary>
    /// <param name="waveId">The wave.</param>
    /// <param name="cancellationToken">Cancels the step.</param>
    /// <returns>Whether a step was carried out, and if not, why.</returns>
    public async Task<StepResult> RunAsync(Guid waveId, CancellationToken cancellationToken)
    {
        PromotionWave wave;
        DueStep due;
        IReadOnlyDictionary<Guid, int> arrivals;
        IReadOnlyDictionary<Guid, int> held;
        await using (var transaction = await locks.BeginSharedAsync(cancellationToken))
        {
            if (await locks.FindWaveForUpdateSkippingLockedAsync(waveId, cancellationToken) is not { } locked
                || DueStep.Next(locked, calendar, await HasArrivalsAsync(waveId, cancellationToken), await HasAdmittedAsync(waveId, locked.ProcessedStage, cancellationToken)) is not { } next
                || await ApplyAsync(locked, next, cancellationToken) is not { } applied)
            {
                return StepResult.Idle;
            }

            arrivals = next.Kind is DueKind.NotifyCommissions or DueKind.ReachHeld || next is { Kind: DueKind.EndStage, Stage: PromotionStage.Rating or PromotionStage.Artifacts }
                ? await actions.TakeArrivalsAsync(waveId, cancellationToken)
                : new Dictionary<Guid, int>();
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            wave = locked;
            due = next;
            held = applied;
        }

        await TellAsync(wave, due, arrivals, held, cancellationToken);
        return StepResult.Done;
    }

    private Task<bool> HasArrivalsAsync(Guid waveId, CancellationToken cancellationToken) =>
        context.Set<PromotionApplication>().AnyAsync(application => application.WaveId == waveId && application.ReviewLetterPending, cancellationToken);

    /// <summary>
    /// Whether some admitted application can move now: one that meets the mandatory requirements, or any once the artifact stage has ended. Before that
    /// the others wait for the deadline or for the candidate's word.
    /// </summary>
    private Task<bool> HasAdmittedAsync(Guid waveId, int processedStage, CancellationToken cancellationToken) =>
        context.Set<PromotionApplication>().AnyAsync(
            application => application.WaveId == waveId
                && application.Status == ApplicationStatus.Admitted
                && application.MeetsAfterRating != null
                && (application.MeetsAfterRating == true || processedStage >= PromotionStage.Artifacts),
            cancellationToken);

    /// <summary>
    /// Makes the changes of a step.
    /// </summary>
    /// <returns>The number of applications by each unit that the step newly held for want of a chair; null when nothing changed, as a step that handed nothing over leaves nothing to commit.</returns>
    private async Task<IReadOnlyDictionary<Guid, int>?> ApplyAsync(PromotionWave wave, DueStep due, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, int> held = new Dictionary<Guid, int>();
        switch (due.Kind)
        {
            case DueKind.StartFirstStage:
                await actions.FixVersionsAsync(wave, cancellationToken);
                wave.VersionsFixed = true;
                break;
            case DueKind.RemindRaters:
                wave.RaterReminderSent = true;
                break;
            case DueKind.EndStage:
                held = await EndAsync(wave, due.Stage, cancellationToken);
                wave.ProcessedStage = due.Stage;
                break;
            case DueKind.ReachHeld:
                var reach = await actions.ReachCommissionAsync(wave, isDeadline: wave.ProcessedStage >= PromotionStage.Artifacts, cancellationToken);
                return reach.Handed > 0 || reach.Held.Count > 0 ? reach.Held : null;
        }

        return held;
    }

    private async Task<IReadOnlyDictionary<Guid, int>> EndAsync(PromotionWave wave, int stage, CancellationToken cancellationToken)
    {
        switch (stage)
        {
            case PromotionStage.FormalCheck:
                await actions.CheckFormalAsync(wave, cancellationToken);
                break;
            case PromotionStage.Rating:
                await actions.CheckRatingAsync(wave, cancellationToken);
                return (await actions.ReachCommissionAsync(wave, isDeadline: false, cancellationToken)).Held;
            case PromotionStage.Artifacts:
                return (await actions.ReachCommissionAsync(wave, isDeadline: true, cancellationToken)).Held;
            case PromotionStage.Review:
                await actions.CloseReviewAsync(wave, cancellationToken);
                break;
        }

        return new Dictionary<Guid, int>();
    }

    /// <summary>
    /// Queues the letters of a committed step. A failure to compose them is logged and does not stop the wave from catching up.
    /// </summary>
    private async Task TellAsync(PromotionWave wave, DueStep due, IReadOnlyDictionary<Guid, int> arrivals, IReadOnlyDictionary<Guid, int> held, CancellationToken cancellationToken)
    {
        try
        {
            await LettersOf(wave, due, arrivals, cancellationToken);
            await mail.NoChairAsync(wave, held, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "The letters of step {Kind} of stage {Stage} of wave {WaveId} were not queued.", due.Kind, due.Stage, wave.Id);
        }
    }

    private Task LettersOf(PromotionWave wave, DueStep due, IReadOnlyDictionary<Guid, int> arrivals, CancellationToken cancellationToken) => (due.Kind, due.Stage) switch
    {
        (DueKind.StartFirstStage, _) => mail.ApplicationsOpenedAsync(wave, cancellationToken),
        (DueKind.RemindRaters, _) => mail.RatingReminderAsync(wave, cancellationToken),
        (DueKind.NotifyCommissions or DueKind.ReachHeld, _) => mail.ReviewArrivedAsync(wave, arrivals, cancellationToken),
        (_, PromotionStage.Submission) => mail.FormalCheckStartedAsync(wave, cancellationToken),
        (_, PromotionStage.FormalCheck) => mail.FormalCheckFinishedAsync(wave, cancellationToken),
        (_, PromotionStage.Rating or PromotionStage.Artifacts) => mail.ReviewArrivedAsync(wave, arrivals, cancellationToken),
        (_, PromotionStage.Review) => mail.ResultsPublishedAsync(wave, cancellationToken),
        (_, PromotionStage.Feedback) => mail.FeedbackFinishedAsync(wave, cancellationToken),
        _ => Task.CompletedTask,
    };
}
