using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// A career level, a part of one track and kept by the track's unit. The track never changes, because matrices are built from a track's levels.
/// The salary range is read and changed apart from the other fields, because only some readers may see it.
/// </summary>
[Audited]
internal sealed class CareerLevel : CatalogEntry
{
    public const int CodeMaxLength = 50;
    public const int RoleDescriptionMaxLength = 4000;

    [Audited]
    public Guid TrackId { get; init; }

    public CareerTrack Track { get; private set; } = null!;

    [Audited]
    public required string Code { get; set; }

    [Audited]
    public bool IsManagerial { get; set; }

    [Audited]
    public DateOnly? ValidFrom { get; set; }

    [Audited]
    public DateOnly? ValidTo { get; set; }

    [Audited]
    public string? RoleDescription { get; set; }

    [Audited]
    public Guid? PreviousLevelId { get; set; }

    /// <summary>
    /// The levels a holder may move on to, in this track or another. Stored on the level itself, so changing them changes the level's version.
    /// </summary>
    [Audited]
    public List<Guid> NextLevelIds { get; set; } = [];

    [Audited]
    public decimal? SalaryFrom { get; set; }

    [Audited]
    public decimal? SalaryTo { get; set; }

    /// <inheritdoc />
    public override Guid? UnitId => Track.OwnerOrgUnitId;
}
