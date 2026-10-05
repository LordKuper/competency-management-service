using Competency.Platform;
using NpgsqlTypes;

namespace Competency.OrgStructure;

/// <summary>
/// A person in the organization directory, belonging to exactly one unit.
/// Employees are never deleted; leaving the organization is the inactive status.
/// </summary>
[Audited]
internal sealed class Employee : EntityBase
{
    public const int FullNameMaxLength = 200;
    public const int PersonnelNumberMaxLength = 50;
    public const int EmailMaxLength = 254;
    public const int PositionMaxLength = 200;

    [Audited]
    public required string FullName { get; set; }

    /// <summary>
    /// The unique personnel number; visible to global administrators only.
    /// </summary>
    [Audited]
    public required string PersonnelNumber { get; set; }

    /// <summary>
    /// The work e-mail address; unique regardless of letter case.
    /// </summary>
    [Audited]
    public required string Email { get; set; }

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
    /// The lower-cased <see cref="Email"/>, maintained by the database so one unique index enforces case-insensitive uniqueness.
    /// </summary>
    public string NormalizedEmail { get; private set; } = null!;

    /// <summary>
    /// The Russian full-text index of <see cref="FullName"/> and <see cref="Position"/>, maintained by the database.
    /// </summary>
    public NpgsqlTsVector SearchVector { get; private set; } = null!;
}
