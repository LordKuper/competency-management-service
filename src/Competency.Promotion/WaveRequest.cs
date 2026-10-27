namespace Competency.Promotion;

/// <summary>
/// A wave to announce or a new state of an announced wave: when it starts, when each stage ends and which units take part. Everything is replaced.
/// </summary>
internal sealed record WaveRequest
{
    /// <summary>
    /// The first day of the first stage.
    /// </summary>
    public required DateOnly StartsOn { get; init; }

    /// <summary>
    /// The last day of each of the seven stages, in the order of the stages.
    /// </summary>
    public required DateOnly[] EndsOn { get; init; }

    /// <summary>
    /// The units of the wave; exactly the listed ones take part.
    /// </summary>
    public required Guid[] UnitIds { get; init; }
}
