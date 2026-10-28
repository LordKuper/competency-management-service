using Competency.Platform;
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
    private const int ReviewOpensAfterStage = 4;

    /// <summary>
    /// Carries out the next step a wave is owed. The commission stage does not begin while a commission that would review an application has no
    /// chair who acts: the step waits, and the wave is tried again on the next call.
    /// </summary>
    /// <param name="waveId">The wave.</param>
    /// <param name="cancellationToken">Cancels the step.</param>
    /// <returns>Whether a step was carried out, and if not, why.</returns>
    public async Task<StepResult> RunAsync(Guid waveId, CancellationToken cancellationToken)
    {
        PromotionWave wave;
        DueStep due;
        await using (var transaction = await locks.BeginSharedAsync(cancellationToken))
        {
            if (await locks.FindWaveForUpdateSkippingLockedAsync(waveId, cancellationToken) is not { } locked
                || DueStep.Next(locked, calendar) is not { } next)
            {
                return StepResult.Idle;
            }

            if (next is { Kind: DueKind.EndStage, Stage: ReviewOpensAfterStage }
                && await actions.UnitsWithoutChairAsync(locked, cancellationToken) is { Count: > 0 } chairless)
            {
                logger.LogWarning("The commission stage of wave {WaveId} waits for a chair in: {Units}", waveId, string.Join(", ", chairless));
                return StepResult.Held;
            }

            await ApplyAsync(locked, next, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            wave = locked;
            due = next;
        }

        await TellAsync(wave, due, cancellationToken);
        return StepResult.Done;
    }

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
            default:
                await EndAsync(wave, due.Stage, cancellationToken);
                wave.ProcessedStage = due.Stage;
                break;
        }
    }

    private Task EndAsync(PromotionWave wave, int stage, CancellationToken cancellationToken) => stage switch
    {
        2 => actions.CheckFormalAsync(wave, cancellationToken),
        3 => actions.CheckRatingAsync(wave, cancellationToken),
        ReviewOpensAfterStage => actions.OpenReviewAsync(wave, cancellationToken),
        5 => actions.CloseReviewAsync(wave, cancellationToken),
        _ => Task.CompletedTask,
    };

    /// <summary>
    /// Queues the letters of a committed step. A failure to compose them is logged and does not stop the wave from catching up.
    /// </summary>
    private async Task TellAsync(PromotionWave wave, DueStep due, CancellationToken cancellationToken)
    {
        try
        {
            await LettersOf(wave, due, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "The letters of step {Kind} of stage {Stage} of wave {WaveId} were not queued.", due.Kind, due.Stage, wave.Id);
        }
    }

    private Task LettersOf(PromotionWave wave, DueStep due, CancellationToken cancellationToken) => (due.Kind, due.Stage) switch
    {
        (DueKind.StartFirstStage, _) => mail.ApplicationsOpenedAsync(wave, cancellationToken),
        (DueKind.RemindRaters, _) => mail.RatingReminderAsync(wave, cancellationToken),
        (_, 1) => mail.FormalCheckStartedAsync(wave, cancellationToken),
        (_, 2) => mail.FormalCheckFinishedAsync(wave, cancellationToken),
        (_, ReviewOpensAfterStage) => mail.ReviewStartedAsync(wave, cancellationToken),
        (_, 5) => mail.ReviewFinishedAsync(wave, cancellationToken),
        (_, 6) => mail.FeedbackStartedAsync(wave, cancellationToken),
        (_, 7) => mail.FeedbackFinishedAsync(wave, cancellationToken),
        _ => Task.CompletedTask,
    };
}
