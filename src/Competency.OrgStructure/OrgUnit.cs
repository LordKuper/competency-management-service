using Competency.Platform;
using NpgsqlTypes;

namespace Competency.OrgStructure;

/// <summary>
/// A node of the organization tree: a unit has at most one parent, so units form a forest without cycles.
/// Units are never deleted, only deactivated.
/// </summary>
[Audited]
internal sealed class OrgUnit : EntityBase
{
    public const int NameMaxLength = 200;

    [Audited]
    public required string Name { get; set; }

    /// <summary>
    /// The parent unit; absent for a root, and several roots are allowed.
    /// </summary>
    [Audited]
    public Guid? ParentId { get; set; }

    /// <summary>
    /// The employee who heads the unit: a working employee of this unit, released when they leave the unit or stop working.
    /// </summary>
    [Audited]
    public Guid? HeadEmployeeId { get; set; }

    [Audited]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The Russian full-text index of <see cref="Name"/>, maintained by the database.
    /// </summary>
    public NpgsqlTsVector SearchVector { get; private set; } = null!;
}
