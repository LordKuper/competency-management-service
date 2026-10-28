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
    /// Reminds the raters, once, shortly before the rating stage ends.
    /// </summary>
    RemindRaters,
}

/// <summary>
/// The next thing a wave is owed, by its dates and the clock. A wave catches up one step at a time, in order, so a stage that ended while the
/// service was down gets its end actions exactly once, after those of the stages before it.
/// </summary>
/// <param name="Kind">What the step does.</param>
/// <param name="Stage">The stage it belongs to, 1 to 7.</param>
internal readonly record struct DueStep(DueKind Kind, int Stage)
{
    /// <summary>
    /// The stage of the circular rating.
    /// </summary>
    public const int RatingStage = 3;

    /// <summary>
    /// How close to the end of the rating stage the raters are reminded.
    /// </summary>
    public static readonly TimeSpan ReminderLead = TimeSpan.FromDays(2);

    /// <summary>
    /// Finds the step a wave is owed now.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="calendar">The clock of the organization.</param>
    /// <returns>The step; <see langword="null"/> when the wave is up to date.</returns>
    public static DueStep? Next(PromotionWave wave, PromotionCalendar calendar)
    {
        var stage = calendar.StageOf(wave);
        if (!wave.VersionsFixed)
        {
            return stage >= 1 ? new DueStep(DueKind.StartFirstStage, 1) : null;
        }

        if (wave.ProcessedStage < PromotionWave.StageCount && stage > wave.ProcessedStage + 1)
        {
            return new DueStep(DueKind.EndStage, wave.ProcessedStage + 1);
        }

        return stage == RatingStage && !wave.RaterReminderSent && calendar.TimeLeft(wave, RatingStage) < ReminderLead
            ? new DueStep(DueKind.RemindRaters, RatingStage)
            : null;
    }
}
