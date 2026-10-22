using Competency.Platform;
using NpgsqlTypes;

namespace Competency.OrgStructure;

/// <summary>
/// A person in the organization directory, belonging to exactly one unit.
/// An employee who stops working is kept with the inactive status; an administrator may also delete the employee for good.
/// An employee has no e-mail of its own: the e-mail belongs to the account bound to the employee, if there is one.
/// </summary>
[Audited]
internal sealed class Employee : EntityBase
{
    public const int NameMaxLength = 100;

    [Audited]
    public required string LastName { get; set; }

    [Audited]
    public required string FirstName { get; set; }

    /// <summary>
    /// The patronymic; null when the person has none or it is not known.
    /// </summary>
    [Audited]
    public string? MiddleName { get; set; }

    /// <summary>
    /// The display name, last name first, derived by the database from the three name parts; it is never written, so the journal records the parts only.
    /// </summary>
    public string FullName { get; private set; } = null!;

    /// <summary>
    /// The position from the catalogue. It must be active and allowed in the unit when it is given or the employee is transferred,
    /// and stays as it is when the position is later deactivated or taken off the unit's list.
    /// </summary>
    [Audited]
    public Guid PositionId { get; set; }

    public Position Position { get; set; } = null!;

    /// <summary>
    /// Whether the employee works in the organization; visible to global administrators and HR.
    /// </summary>
    [Audited]
    public bool IsActive { get; set; } = true;

    [Audited]
    public Guid OrgUnitId { get; set; }

    public OrgUnit OrgUnit { get; set; } = null!;

    /// <summary>
    /// The Russian full-text index of the name parts, maintained by the database; the position is searched through <see cref="OrgStructure.Position.SearchVector"/>.
    /// </summary>
    public NpgsqlTsVector SearchVector { get; private set; } = null!;
}
