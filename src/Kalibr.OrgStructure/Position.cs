using Competency.Platform;
using NpgsqlTypes;

namespace Competency.OrgStructure;

/// <summary>
/// An entry of the shared position catalogue that employees' positions refer to. Names are stored trimmed and are unique
/// regardless of letter case, which the database enforces with a unique index on <c>lower(name)</c>.
/// Positions are never deleted, only deactivated: a deactivated position stays with the employees who hold it,
/// but cannot be given to anyone else.
/// </summary>
[Audited]
internal sealed class Position : EntityBase
{
    public const int NameMaxLength = 200;

    [Audited]
    public required string Name { get; set; }

    [Audited]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The Russian full-text index of <see cref="Name"/>, maintained by the database; employee search matches positions through it.
    /// </summary>
    public NpgsqlTsVector SearchVector { get; private set; } = null!;
}
