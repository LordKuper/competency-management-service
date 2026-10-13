namespace Competency.CareerFramework;

/// <summary>
/// What a published matrix requires of a competency at a level, with names as they stood at publication.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="CompetencyName">The competency's name.</param>
/// <param name="RequiredScore">The required point of the scale; null when the requirement sets none.</param>
/// <param name="ScoreLabel">What the required point means; null when the requirement sets none.</param>
/// <param name="Type">How the competency counts towards readiness.</param>
internal sealed record MyPlaceRequirementResponse(Guid CompetencyId, string CompetencyName, int? RequiredScore, string? ScoreLabel, CompetencyType Type);
