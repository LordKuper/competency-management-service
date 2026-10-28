namespace Competency.Promotion;

/// <summary>
/// A wave as the API returns it to those who manage it.
/// </summary>
/// <param name="Id">The wave.</param>
/// <param name="StartsOn">The first day of the first stage.</param>
/// <param name="EndsOn">The last day of each stage, in the order of the stages.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 to 6 while a stage runs, 7 once the last stage has ended.</param>
/// <param name="IsFinished">Whether the wave is over and its last stage processed; its units are free for another wave.</param>
/// <param name="AuthorId">The account that announced the wave.</param>
/// <param name="AuthorName">The name of the author: their employee's, or else the e-mail address of the account.</param>
/// <param name="Units">The units of the wave in name order.</param>
internal sealed record WaveResponse(
    Guid Id,
    DateOnly StartsOn,
    IReadOnlyList<DateOnly> EndsOn,
    int Stage,
    bool IsFinished,
    Guid AuthorId,
    string AuthorName,
    IReadOnlyList<WaveUnitResponse> Units);
