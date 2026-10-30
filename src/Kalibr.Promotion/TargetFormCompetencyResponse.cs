using Kalibr.CareerFramework;

namespace Kalibr.Promotion;

/// <summary>
/// A competency of the target form and of the rater's form, as the bound version froze it.
/// </summary>
/// <param name="Id">The competency.</param>
/// <param name="Name">The competency name.</param>
/// <param name="Description">What the competency means; null when the version holds none.</param>
/// <param name="Type">How the competency counts towards readiness.</param>
/// <param name="Block">The block the competency belongs to with its place among the blocks; null for a competency outside blocks.</param>
/// <param name="ScalePoints">The points that can be given, each with what reaching it means.</param>
/// <param name="Requirements">The score each level requires; empty where the form is not shown the requirements.</param>
internal sealed record TargetFormCompetencyResponse(
    Guid Id,
    string Name,
    string? Description,
    CompetencyType Type,
    CareerVersionBlock? Block,
    IReadOnlyList<ScalePoint> ScalePoints,
    IReadOnlyList<LevelRequirementResponse> Requirements)
{
    /// <summary>
    /// Describes a competency of a version.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="competency">The competency.</param>
    /// <param name="withRequirements">Whether to list the scores the levels require.</param>
    /// <returns>The competency of the form.</returns>
    public static TargetFormCompetencyResponse Of(CareerVersion version, CareerVersionCompetency competency, bool withRequirements) =>
        new(
            competency.Id,
            competency.Name,
            competency.Description,
            competency.Type,
            version.Blocks.FirstOrDefault(block => block.Id == competency.BlockId),
            competency.ScalePoints,
            withRequirements
                ? [.. version.Requirements.Where(requirement => requirement.CompetencyId == competency.Id).Select(requirement => new LevelRequirementResponse(requirement.LevelId, requirement.RequiredScore))]
                : []);
}
