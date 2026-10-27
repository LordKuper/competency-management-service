using Competency.Platform;
using Microsoft.Extensions.Options;

namespace Competency.Promotion;

/// <summary>
/// Tells which day it is and which stage a wave is in, in the time zone of the organization: a stage ends with the last moment of its last day there.
/// Safe for concurrent use.
/// </summary>
internal sealed class PromotionCalendar(TimeProvider time, IOptions<AppOptions> app)
{
    /// <summary>
    /// The stage number of a wave whose last stage has ended.
    /// </summary>
    public const int AfterLastStage = PromotionWave.StageCount + 1;

    private readonly TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(app.Value.TimeZone);

    /// <summary>
    /// The current day in the organization's time zone.
    /// </summary>
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), zone).DateTime);

    /// <summary>
    /// Finds the moment a day begins in the organization's time zone.
    /// </summary>
    /// <param name="day">The day.</param>
    /// <returns>The first moment of the day; when the zone skips midnight that day, the first moment after the gap.</returns>
    public DateTimeOffset StartOf(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        if (zone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }

    /// <summary>
    /// Finds the stage a wave is in now.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <returns>0 before the wave starts, 1 to 7 for a running stage, <see cref="AfterLastStage"/> once the last stage has ended.</returns>
    public int StageOf(PromotionWave wave) => StageAt(wave, time.GetUtcNow());

    /// <summary>
    /// Finds the stage a wave is in at a moment. A stage whose last day is the last day of the previous one lasts no time and is never current.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="moment">The moment.</param>
    /// <returns>0 before the wave starts, 1 to 7 for a running stage, <see cref="AfterLastStage"/> once the last stage has ended.</returns>
    public int StageAt(PromotionWave wave, DateTimeOffset moment)
    {
        if (moment < StartOf(wave.StartsOn))
        {
            return 0;
        }

        for (var stage = 1; stage <= PromotionWave.StageCount; stage++)
        {
            if (moment < StartOf(wave.EndsOn[stage - 1].AddDays(1)))
            {
                return stage;
            }
        }

        return AfterLastStage;
    }
}
