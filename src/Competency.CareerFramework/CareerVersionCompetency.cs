namespace Competency.CareerFramework;

/// <summary>
/// A competency of a matrix version with its place in the rating and its scale as the version froze it.
/// </summary>
/// <param name="Id">The competency identifier.</param>
/// <param name="Name">The competency name.</param>
/// <param name="BlockId">The shared block the competency counts in; null for the competencies outside blocks.</param>
/// <param name="Type">How the competency counts towards readiness in the version.</param>
/// <param name="MaxScore">The highest score of the competency's scale; 0 when the competency has no scale points.</param>
/// <param name="ScalePoints">The points of the competency's scale.</param>
public sealed record CareerVersionCompetency(
    Guid Id,
    string Name,
    Guid? BlockId,
    CompetencyType Type,
    int MaxScore,
    IReadOnlyList<ScalePoint> ScalePoints);
