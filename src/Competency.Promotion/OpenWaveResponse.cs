namespace Competency.Promotion;

/// <summary>
/// An upcoming wave or a wave taking applications, as every signed-in user sees it.
/// </summary>
/// <param name="Id">The wave.</param>
/// <param name="StartsOn">The first day of the first stage.</param>
/// <param name="EndsOn">The last day of each stage, in the order of the stages.</param>
/// <param name="Stage">The stage the wave is in now: 0 before it starts, 1 while it takes applications.</param>
/// <param name="Units">The units of the wave in name order, each with what an application can aim at.</param>
internal sealed record OpenWaveResponse(
    Guid Id,
    DateOnly StartsOn,
    IReadOnlyList<DateOnly> EndsOn,
    int Stage,
    IReadOnlyList<OpenWaveUnitResponse> Units);
