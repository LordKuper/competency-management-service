using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// A career track of one direction, such as backend development. The direction never changes, because matrices name both.
/// </summary>
[Audited]
internal sealed class CareerTrack : CatalogEntry
{
    [Audited]
    public Guid DirectionId { get; init; }
}
