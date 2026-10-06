using Competency.Platform;
using NpgsqlTypes;

namespace Competency.OrgStructure;

/// <summary>
/// A person in the organization directory, belonging to exactly one unit.
/// Employees are never deleted; leaving the organization is the inactive status.
/// An employee has no e-mail of its own: the e-mail belongs to the account bound to the employee, if there is one.
/// </summary>
[Audited]
internal sealed class Employee : EntityBase
{
    public const int FullNameMaxLength = 200;
    public const int PositionMaxLength = 200;

    [Audited]
    public required string FullName { get; set; }

    /// <summary>
    /// The position as free text.
    /// </summary>
    [Audited]
    public required string Position { get; set; }

    /// <summary>
    /// Whether the employee works in the organization; visible to global administrators only.
    /// </summary>
    [Audited]
    public bool IsActive { get; set; } = true;

    [Audited]
    public Guid OrgUnitId { get; set; }

    public OrgUnit OrgUnit { get; set; } = null!;

    /// <summary>
    /// The Russian full-text index of <see cref="FullName"/> and <see cref="Position"/>, maintained by the database.
    /// </summary>
    public NpgsqlTsVector SearchVector { get; private set; } = null!;
}
