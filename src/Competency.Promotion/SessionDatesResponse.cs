namespace Competency.Promotion;

/// <summary>
/// The dates of a career session, for every role that sees the session or an application in it, so a deadline can be shown beside what it limits.
/// </summary>
/// <param name="WaveId">The wave.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 to 6 while a stage runs, 7 once the last stage has ended.</param>
/// <param name="StartsOn">The first day of the first stage.</param>
/// <param name="EndsOn">The last day of each stage, in the order of the stages.</param>
/// <param name="ResultsPublishedOn">The day the results are published, the last day of the commission stage.</param>
internal sealed record SessionDatesResponse(Guid WaveId, int Stage, DateOnly StartsOn, IReadOnlyList<DateOnly> EndsOn, DateOnly ResultsPublishedOn)
{
    /// <summary>
    /// Describes the dates of a wave.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="stage">The stage the wave is in now, as <see cref="PromotionCalendar.StageOf"/> finds it.</param>
    /// <returns>The dates.</returns>
    public static SessionDatesResponse From(PromotionWave wave, int stage) => new(wave.Id, stage, wave.StartsOn, wave.EndsOn, wave.ResultsPublishedOn);
}
