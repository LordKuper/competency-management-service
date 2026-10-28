namespace Competency.Promotion;

/// <summary>
/// The score a level requires of a competency.
/// </summary>
/// <param name="LevelId">The level.</param>
/// <param name="RequiredScore">The required point of the competency's scale.</param>
internal sealed record LevelRequirementResponse(Guid LevelId, int RequiredScore);
