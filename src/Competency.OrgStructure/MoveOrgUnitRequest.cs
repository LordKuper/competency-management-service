namespace Competency.OrgStructure;

/// <summary>
/// The new parent of a unit; <see langword="null"/> makes it a root. The field is required so that omitting it never detaches a unit by accident.
/// </summary>
internal sealed record MoveOrgUnitRequest
{
    public required Guid? ParentId { get; init; }
}
