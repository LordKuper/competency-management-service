namespace Kalibr.OrgStructure;

/// <summary>
/// A position allowed in a unit, with the unit whose list holds it: the requested unit itself for its own list,
/// or a unit above it for an inherited one. A position held by several of these lists appears once per list.
/// </summary>
internal sealed record OrgUnitPositionResponse(Guid PositionId, string Name, bool IsActive, Guid OrgUnitId, string OrgUnitName);
