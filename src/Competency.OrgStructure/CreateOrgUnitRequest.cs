namespace Competency.OrgStructure;

/// <summary>
/// A new unit. It starts active, so its parent must be active too; an absent parent makes it a root.
/// It has no employees yet, so it has no head either: one is assigned once employees have been added.
/// </summary>
internal sealed record CreateOrgUnitRequest
{
    public required string Name { get; init; }

    public Guid? ParentId { get; init; }

    public Dictionary<string, string[]> Validate() => OrgUnitInput.Validate(Name);
}
