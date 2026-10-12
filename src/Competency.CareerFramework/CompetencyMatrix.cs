using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// The competency matrix of one unit for one career track; a unit has at most one per track. Its content lives in its versions.
/// The unit, direction and track never change, because every version is built for them.
/// </summary>
[Audited]
internal sealed class CompetencyMatrix : EntityBase
{
    [Audited]
    public Guid OrgUnitId { get; init; }

    [Audited]
    public Guid DirectionId { get; init; }

    [Audited]
    public Guid TrackId { get; init; }
}
