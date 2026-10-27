using Competency.Platform;

namespace Competency.OrgStructure;

/// <summary>
/// A legal entity of a secondary assignment.
/// </summary>
[Audited]
internal sealed class AssignmentLegalEntity
{
    [Audited]
    public Guid AssignmentId { get; init; }

    [Audited]
    public Guid LegalEntityId { get; init; }

    public EmployeeAssignment Assignment { get; init; } = null!;

    public LegalEntity LegalEntity { get; init; } = null!;
}
