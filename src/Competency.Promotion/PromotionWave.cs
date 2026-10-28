using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// A promotion wave: the units whose employees may apply and the dates of its seven stages. The stage a wave is in follows from the dates
/// and the clock and is never stored; the stored stage is only how far the actions of finished stages have been carried out.
/// </summary>
[Audited]
internal sealed class PromotionWave : EntityBase
{
    public const int StageCount = 7;

    /// <summary>
    /// The account that announced the wave.
    /// </summary>
    [Audited]
    public Guid AuthorId { get; init; }

    /// <summary>
    /// The first day of the first stage.
    /// </summary>
    [Audited]
    public DateOnly StartsOn { get; set; }

    /// <summary>
    /// The last day of each stage, in the order of the stages. A stage runs from the day after the end of the previous one to the end of this day.
    /// </summary>
    [Audited]
    public DateOnly[] EndsOn { get; set; } = [];

    /// <summary>
    /// The last stage whose end actions have been carried out, from 0 for none to <see cref="StageCount"/>.
    /// </summary>
    [Audited]
    public int ProcessedStage { get; set; }

    /// <summary>
    /// Whether the matrix versions of the units have been bound, which is done once when the first stage starts; <see cref="ProcessedStage"/>
    /// of 0 cannot tell a wave that has not started from one that has.
    /// </summary>
    [Audited]
    public bool VersionsFixed { get; set; }

    /// <summary>
    /// Whether the raters have been reminded to answer, which is done once.
    /// </summary>
    [Audited]
    public bool RaterReminderSent { get; set; }

    /// <summary>
    /// Whether the wave is over, its last stage processed; the units of an unfinished wave belong to no other wave.
    /// </summary>
    public bool IsFinished => ProcessedStage >= StageCount;
}
