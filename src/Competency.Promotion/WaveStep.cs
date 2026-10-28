using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Competency.Promotion;

/// <summary>
/// How an attempt to carry out the next step of a wave ended.
/// </summary>
internal enum StepResult
{
    /// <summary>
    /// Nothing was due, or another transaction was changing the wave.
    /// </summary>
    Idle,

    /// <summary>
    /// A step was carried out and committed.
    /// </summary>
    Done,

    /// <summary>
    /// A step is due but must wait for a precondition; nothing was changed.
    /// </summary>
    Held,
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
    /// Carries out the next step a wave is owed. Applications reach a commission only if it has a chair who acts: the end of the rating stage, which
    /// finds who meets the requirements, and the end of the artifact stage wait for one, and the wave is tried again on the next call.
    /// </summary>
    /// <param name="waveId">The wave.</param>
    /// <param name="cancellationToken">Cancels the step.</param>
    /// <returns>Whether a step was carried out, and if not, why.</returns>
    public async Task<StepResult> RunAsync(Guid waveId, CancellationToken cancellationToken)
    {
        PromotionWave wave;
        DueStep due;
        IReadOnlyDictionary<Guid, int> arrivals;
        await using (var transaction = await locks.BeginSharedAsync(cancellationToken))
        {
            if (await locks.FindWaveForUpdateSkippingLockedAsync(waveId, cancellationToken) is not { } locked
                || DueStep.Next(locked, calendar, await HasArrivalsAsync(waveId, cancellationToken)) is not { } next)
            {
                return StepResult.Idle;
            }

            if (next is { Kind: DueKind.EndStage, Stage: PromotionStage.Rating or PromotionStage.Artifacts }
                && await actions.UnitsWithoutChairAsync(locked, cancellationToken) is { Count: > 0 } chairless)
            {
                logger.LogWarning("Applications of wave {WaveId} wait for a chair in: {Units}", waveId, string.Join(", ", chairless));
                return StepResult.Held;
            }

            await ApplyAsync(locked, next, cancellationToken);
            arrivals = next.Kind == DueKind.NotifyCommissions || next is { Kind: DueKind.EndStage, Stage: PromotionStage.Rating or PromotionStage.Artifacts }
                ? await actions.TakeArrivalsAsync(waveId, cancellationToken)
                : new Dictionary<Guid, int>();
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            wave = locked;
            due = next;
        }

        await TellAsync(wave, due, arrivals, cancellationToken);
        return StepResult.Done;
    }

    private Task<bool> HasArrivalsAsync(Guid waveId, CancellationToken cancellationToken) =>
        context.Set<PromotionApplication>().AnyAsync(application => application.WaveId == waveId && application.ReviewLetterPending, cancellationToken);

    private async Task ApplyAsync(PromotionWave wave, DueStep due, CancellationToken cancellationToken)
    {
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
                await EndAsync(wave, due.Stage, cancellationToken);
                wave.ProcessedStage = due.Stage;
                break;
        }
    }

    private async Task EndAsync(PromotionWave wave, int stage, CancellationToken cancellationToken)
    {
        switch (stage)
        {
            case PromotionStage.FormalCheck:
                await actions.CheckFormalAsync(wave, cancellationToken);
                break;
            case PromotionStage.Rating:
                await actions.CheckRatingAsync(wave, cancellationToken);
                await actions.ReachCommissionAsync(wave, isDeadline: false, cancellationToken);
                break;
            case PromotionStage.Artifacts:
                await actions.ReachCommissionAsync(wave, isDeadline: true, cancellationToken);
                break;
            case PromotionStage.Review:
                await actions.CloseReviewAsync(wave, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Queues the letters of a committed step. A failure to compose them is logged and does not stop the wave from catching up.
    /// </summary>
    private async Task TellAsync(PromotionWave wave, DueStep due, IReadOnlyDictionary<Guid, int> arrivals, CancellationToken cancellationToken)
    {
        try
        {
            await LettersOf(wave, due, arrivals, cancellationToken);
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
        (DueKind.NotifyCommissions, _) => mail.ReviewArrivedAsync(wave, arrivals, cancellationToken),
        (_, PromotionStage.Submission) => mail.FormalCheckStartedAsync(wave, cancellationToken),
        (_, PromotionStage.FormalCheck) => mail.FormalCheckFinishedAsync(wave, cancellationToken),
        (_, PromotionStage.Rating or PromotionStage.Artifacts) => mail.ReviewArrivedAsync(wave, arrivals, cancellationToken),
        (_, PromotionStage.Review) => mail.ResultsPublishedAsync(wave, cancellationToken),
        (_, PromotionStage.Feedback) => mail.FeedbackFinishedAsync(wave, cancellationToken),
        _ => Task.CompletedTask,
    };
}
