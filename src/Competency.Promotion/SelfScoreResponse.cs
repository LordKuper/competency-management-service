using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// The score a candidate gave a competency.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="CompetencyName">The competency name as the bound version froze it.</param>
/// <param name="Description">What the competency means as the version froze it; null when it holds none.</param>
/// <param name="Type">How the competency counts towards readiness.</param>
/// <param name="Block">The block the competency belongs to with its place among the blocks; null for a competency outside blocks.</param>
/// <param name="RequiredScore">The score the target level requires; null when it requires none.</param>
/// <param name="ScalePoints">The points of the competency's scale, each with what reaching it means.</param>
/// <param name="Score">A point of the competency's scale.</param>
internal sealed record SelfScoreResponse(
    Guid CompetencyId,
    string CompetencyName,
    string? Description,
    CompetencyType Type,
    CareerVersionBlock? Block,
    int? RequiredScore,
    IReadOnlyList<ScalePoint> ScalePoints,
    int Score);
