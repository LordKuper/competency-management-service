namespace Competency.Promotion;

/// <summary>
/// What a due step of a wave does.
/// </summary>
internal enum DueKind
{
    /// <summary>
    /// Binds the matrix versions as the first stage starts.
    /// </summary>
    StartFirstStage,

    /// <summary>
    /// Carries out the actions at the end of a stage.
    /// </summary>
    EndStage,

    /// <summary>
    /// Tells the commissions, in one letter each, of the applications that reached them since they were last told.
    /// </summary>
    NotifyCommissions,

    /// <summary>
    /// Hands over the applications held back for want of a chair, to the commissions that have one now.
    /// </summary>
    ReachHeld,

    /// <summary>
    /// Reminds the raters, once, shortly before the rating stage ends.
    /// </summary>
    RemindRaters,
}

/// <summary>
/// The next thing a wave is owed, by its dates and the clock. A wave catches up one step at a time, in order, so a stage that ended while the
/// service was down gets its end actions exactly once, after those of the stages before it.
/// </summary>
/// <param name="Kind">What the step does.</param>
/// <param name="Stage">The stage it belongs to, 1 to 6.</param>
internal readonly record struct DueStep(DueKind Kind, int Stage)
{
    /// <summary>
    /// How close to the end of the rating stage the raters are reminded.
    /// </summary>
    public static readonly TimeSpan ReminderLead = TimeSpan.FromDays(2);

    /// <summary>
    /// Finds the step a wave is owed now.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="calendar">The clock of the organization.</param>
    /// <param name="hasArrivals">Whether applications have reached a commission whose members have not been told.</param>
    /// <param name="hasAdmitted">Whether some application of the wave still waits, admitted, to reach a commission. Whether one can is for the step to find out.</param>
    /// <returns>The step; <see langword="null"/> when the wave is up to date.</returns>
    public static DueStep? Next(PromotionWave wave, PromotionCalendar calendar, bool hasArrivals, bool hasAdmitted = false)
    {
        var stage = calendar.StageOf(wave);
        if (!wave.VersionsFixed)
        {
            return stage >= PromotionStage.Submission ? new DueStep(DueKind.StartFirstStage, PromotionStage.Submission) : null;
        }

        if (wave.ProcessedStage < PromotionWave.StageCount && stage > wave.ProcessedStage + 1)
        {
            return new DueStep(DueKind.EndStage, wave.ProcessedStage + 1);
        }

        if (hasArrivals)
        {
            return new DueStep(DueKind.NotifyCommissions, stage);
        }

        if (hasAdmitted && wave.ProcessedStage >= PromotionStage.Rating && stage <= PromotionStage.Review)
        {
            return new DueStep(DueKind.ReachHeld, stage);
        }

        return stage == PromotionStage.Rating && !wave.RaterReminderSent && calendar.TimeLeft(wave, PromotionStage.Rating) < ReminderLead
            ? new DueStep(DueKind.RemindRaters, PromotionStage.Rating)
            : null;
    }
}
