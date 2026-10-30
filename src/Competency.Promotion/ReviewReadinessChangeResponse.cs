namespace Competency.Promotion;

/// <summary>
/// How the candidate's readiness moved since the latest earlier application that got a decision, whatever its target. Both readiness figures come from the
/// raters' averages, so the two applications are compared on the same footing.
/// </summary>
/// <param name="PreviousWaveStartsOn">The first day of the first stage of the wave of that application.</param>
/// <param name="PreviousTarget">The track and level that application aimed at.</param>
/// <param name="PreviousRating">The rating the raters' averages earned then, in percent, unrounded.</param>
/// <param name="Rating">The rating the raters' averages earn for this application, in percent, unrounded.</param>
/// <param name="Delta">The change in percentage points between the two ratings as shown, to one decimal place.</param>
internal sealed record ReviewReadinessChangeResponse(
    DateOnly PreviousWaveStartsOn,
    ApplicationTargetResponse PreviousTarget,
    decimal PreviousRating,
    decimal Rating,
    decimal Delta);
