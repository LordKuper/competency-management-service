using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// A competency of the catalogue, of one direction or, without one, general. Every saved change also adds a <see cref="CompetencyVersion"/>.
/// </summary>
[Audited]
internal sealed class CareerCompetency : CatalogEntry
{
    public const int DescriptionMaxLength = 4000;

    [Audited]
    public string? Description { get; set; }

    /// <summary>
    /// The direction whose matrices may use the competency; null for a general competency any matrix may use.
    /// </summary>
    [Audited]
    public Guid? DirectionId { get; set; }

    [Audited]
    public Guid ScaleId { get; set; }
}
