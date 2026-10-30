using Kalibr.Platform;

namespace Kalibr.OrgStructure;

/// <summary>
/// An entry of the legal entity directory that assignments refer to. Names are stored trimmed and are unique
/// regardless of letter case, which the database enforces with a unique index on <c>lower(name)</c>.
/// Legal entities are never deleted.
/// </summary>
[Audited]
internal sealed class LegalEntity : EntityBase
{
    public const int NameMaxLength = 200;

    [Audited]
    public required string Name { get; set; }
}
