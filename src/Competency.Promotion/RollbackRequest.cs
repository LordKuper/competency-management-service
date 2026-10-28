namespace Competency.Promotion;

/// <summary>
/// The stage a wave goes back to and the dates it is then given: the stage becomes the current one today.
/// </summary>
internal sealed record RollbackRequest
{
    /// <summary>
    /// The stage to go back to, 1 to 7; it must have passed.
    /// </summary>
    public required int Stage { get; init; }

    /// <summary>
    /// The last day of each of the seven stages. The stages before <see cref="Stage"/> keep their dates; the others end today or later.
    /// </summary>
    public required DateOnly[] EndsOn { get; init; }
}
