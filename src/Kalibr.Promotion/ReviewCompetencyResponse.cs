using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// Everything the commission weighs about one competency of a candidate.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="Name">The competency name as the bound version froze it.</param>
/// <param name="Description">What the competency means as the version froze it; null when it holds none.</param>
/// <param name="Type">The type of the competency in the version.</param>
/// <param name="Block">The block the competency belongs to with its place among the blocks; null for a competency outside blocks.</param>
/// <param name="RequiredScore">The score the target level requires; null when it requires none.</param>
/// <param name="ScalePoints">The points the chair can give.</param>
/// <param name="SelfScore">The candidate's own score; null when there is none.</param>
/// <param name="Circular">What the raters answered, in sum.</param>
/// <param name="Change">How the raters' average moved since the latest earlier decided application; null when the candidate has none.</param>
/// <param name="Artifact">The evidence the candidate gave; null when there is none.</param>
/// <param name="IsArtifactMandatory">Whether the candidate had to give evidence for the competency.</param>
/// <param name="DefaultFinalScore">The score the chair's screen starts from: the raters' average rounded to a whole point, the one the default rating counts; null when no rater observed the competency.</param>
/// <param name="FinalScore">The chair's score; null until they give it.</param>
/// <param name="IsMet">Whether the chair's score meets the requirement; null until they give a score or when there is no requirement.</param>
internal sealed record ReviewCompetencyResponse(
    Guid CompetencyId,
    string Name,
    string? Description,
    CompetencyType Type,
    CareerVersionBlock? Block,
    int? RequiredScore,
    IReadOnlyList<ScalePoint> ScalePoints,
    int? SelfScore,
    CircularSummaryResponse Circular,
    ReviewCompetencyChangeResponse? Change,
    ArtifactResponse? Artifact,
    bool IsArtifactMandatory,
    int? DefaultFinalScore,
    int? FinalScore,
    bool? IsMet);
