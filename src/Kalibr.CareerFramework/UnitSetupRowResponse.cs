namespace Competency.CareerFramework;

/// <summary>
/// Whether a unit has one thing it needs, with the reason when it does not.
/// </summary>
/// <param name="Row">The thing.</param>
/// <param name="Status">Whether the unit has it.</param>
/// <param name="Reason">Why the unit lacks it or waits for it; null when the unit has it.</param>
internal sealed record UnitSetupRowResponse(UnitSetupRow Row, UnitSetupStatus Status, UnitSetupReason? Reason = null);
