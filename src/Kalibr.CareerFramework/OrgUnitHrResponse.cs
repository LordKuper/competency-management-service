namespace Kalibr.CareerFramework;

/// <summary>
/// The HR of a unit: the one assigned to the unit itself and the one it would inherit from the nearest unit above with an assignment.
/// The assigned one, when present, is the unit's HR; the inherited one takes over once it is removed.
/// </summary>
internal sealed record OrgUnitHrResponse(OrgUnitHrPersonResponse? Assigned, OrgUnitHrPersonResponse? Inherited);
