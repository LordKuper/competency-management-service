namespace Competency.OrgStructure;

/// <summary>
/// A new unit. It starts active, so its parent and head must be active too; an absent parent makes it a root.
/// </summary>
internal sealed record CreateOrgUnitRequest
{
    public required string Name { get; init; }

    public Guid? ParentId { get; init; }

    public Guid? HeadEmployeeId { get; init; }

    public DateOnly? ValidFrom { get; init; }

    public DateOnly? ValidTo { get; init; }

    public Dictionary<string, string[]> Validate() => OrgUnitInput.Validate(Name, ValidFrom, ValidTo);
}
