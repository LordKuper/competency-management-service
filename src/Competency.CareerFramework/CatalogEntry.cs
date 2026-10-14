using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// An entry of a career catalogue: a direction, track, level, competency category, competency, scale or shared block. Only a track belongs to a unit,
/// and a level to its track's; every other entry belongs to no unit. Names are stored trimmed. An entry still in use is deactivated rather than deleted.
/// </summary>
internal abstract class CatalogEntry : EntityBase
{
    public const int NameMaxLength = 200;

    [Audited]
    public required string Name { get; set; }

    [Audited]
    public CatalogStatus Status { get; set; }

    /// <summary>
    /// The account that created the entry; null for the entries the database starts with.
    /// </summary>
    [Audited]
    public Guid? AuthorId { get; init; }

    /// <summary>
    /// The unit whose head and responsible HR keep the entry; null for an entry no unit keeps, which global administrators and every HR keep.
    /// </summary>
    public virtual Guid? UnitId => null;

    /// <summary>
    /// The unit lists name and filter the entry by: the unit that keeps it, or the unit a shared block applies to; null for none.
    /// </summary>
    public virtual Guid? ListedUnitId => UnitId;
}
