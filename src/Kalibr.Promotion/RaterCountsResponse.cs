namespace Kalibr.Promotion;

/// <summary>
/// How many raters an application lists against what a filed application needs.
/// </summary>
/// <param name="Total">The raters listed.</param>
/// <param name="RequiredTotal">The least a filed application needs.</param>
/// <param name="FromOtherUnits">The raters from units other than the candidate's own.</param>
/// <param name="RequiredFromOtherUnits">The least of them a filed application needs.</param>
/// <param name="MissingTotal">How many raters are still needed; 0 once there are enough.</param>
/// <param name="MissingFromOtherUnits">How many raters from other units are still needed; 0 once there are enough.</param>
/// <param name="PendingEmailCount">The raters without an account whose address is entered and waits for confirmation.</param>
internal sealed record RaterCountsResponse(
    int Total,
    int RequiredTotal,
    int FromOtherUnits,
    int RequiredFromOtherUnits,
    int MissingTotal,
    int MissingFromOtherUnits,
    int PendingEmailCount);
