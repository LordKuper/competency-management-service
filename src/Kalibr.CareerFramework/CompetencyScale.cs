using Kalibr.Platform;

namespace Kalibr.CareerFramework;

/// <summary>
/// A scale that competencies are assessed on, with its points. The points are stored on the scale itself, so changing them changes
/// the scale's version and is journaled with it. Exactly one scale is the default, which a new competency gets unless it names another.
/// </summary>
[Audited]
internal sealed class CompetencyScale : CatalogEntry
{
    public const int MinPoints = 2;
    public const int MaxScore = 100;
    public const int LabelMaxLength = 500;

    /// <summary>
    /// The points in ascending order of score.
    /// </summary>
    [Audited]
    public List<ScalePoint> Points { get; set; } = [];

    [Audited]
    public bool IsDefault { get; set; }
}
