using Kalibr.Platform;

namespace Kalibr.CareerFramework;

/// <summary>
/// A career track of one direction and one unit, such as backend development. Neither the direction nor the unit ever changes,
/// because matrices name both.
/// </summary>
[Audited]
internal sealed class CareerTrack : CatalogEntry
{
    [Audited]
    public required Guid OwnerOrgUnitId { get; init; }

    [Audited]
    public Guid DirectionId { get; init; }

    /// <inheritdoc />
    public override Guid? UnitId => OwnerOrgUnitId;
}
