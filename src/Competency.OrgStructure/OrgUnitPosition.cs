using Competency.Platform;

namespace Competency.OrgStructure;

/// <summary>
/// A position allowed in a unit. An employee may hold any active position allowed in their unit or in a unit above it.
/// Taking a position off the list deletes the row; the employees who hold it keep it.
/// </summary>
[Audited]
internal sealed class OrgUnitPosition
{
    [Audited]
    public Guid OrgUnitId { get; init; }

    [Audited]
    public Guid PositionId { get; init; }

    public OrgUnit OrgUnit { get; init; } = null!;

    public Position Position { get; init; } = null!;
}
