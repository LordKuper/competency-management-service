namespace Competency.OrgStructure;

/// <summary>
/// The new values of a unit's own fields. Every field is required so that omitting one never clears it by accident;
/// the parent and the status change only through their own operations.
/// </summary>
internal sealed record UpdateOrgUnitRequest
{
    public required string Name { get; init; }

    /// <summary>
    /// The unit's head: a working employee of this unit. It is checked only when it differs from the current head,
    /// so that an unrelated edit is never blocked by an earlier head who no longer fits.
    /// </summary>
    public required Guid? HeadEmployeeId { get; init; }

    public Dictionary<string, string[]> Validate() => OrgUnitInput.Validate(Name);
}
