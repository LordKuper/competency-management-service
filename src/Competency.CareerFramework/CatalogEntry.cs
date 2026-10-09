using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// An entry of a career catalogue: a direction, track, level, competency category, competency or scale. An entry belongs to a unit,
/// or to no unit when it is shared. Names are stored trimmed. Entries are never deleted, only deactivated.
/// </summary>
internal abstract class CatalogEntry : EntityBase
{
    public const int NameMaxLength = 200;

    [Audited]
    public required string Name { get; set; }

    /// <summary>
    /// The unit that keeps the entry; null for a shared entry, which only global administrators keep.
    /// </summary>
    [Audited]
    public Guid? OwnerOrgUnitId { get; init; }

    [Audited]
    public CatalogStatus Status { get; set; }

    /// <summary>
    /// The account that created the entry; null for the entries the database starts with.
    /// </summary>
    [Audited]
    public Guid? AuthorId { get; init; }
}
