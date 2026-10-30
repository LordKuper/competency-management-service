using Competency.Platform;

namespace Competency.OrgStructure;

/// <summary>
/// A legal entity of an employee's main assignment. Deleting the employee deletes the link.
/// </summary>
[Audited]
internal sealed class EmployeeLegalEntity
{
    [Audited]
    public Guid EmployeeId { get; init; }

    [Audited]
    public Guid LegalEntityId { get; init; }

    public LegalEntity LegalEntity { get; init; } = null!;
}
