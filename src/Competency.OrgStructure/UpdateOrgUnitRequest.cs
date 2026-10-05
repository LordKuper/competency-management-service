namespace Competency.OrgStructure;

/// <summary>
/// The new values of a unit's own fields. Every field is required so that omitting one never clears it by accident;
/// the parent and the status change only through their own operations.
/// </summary>
internal sealed record UpdateOrgUnitRequest
{
    public required string Name { get; init; }

    public required Guid? HeadEmployeeId { get; init; }

    public required DateOnly? ValidFrom { get; init; }

    public required DateOnly? ValidTo { get; init; }

    public Dictionary<string, string[]> Validate() => OrgUnitInput.Validate(Name, ValidFrom, ValidTo);
}
