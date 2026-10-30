namespace Competency.CareerFramework;

/// <summary>
/// The score a level of a matrix version requires of a competency.
/// </summary>
/// <param name="LevelId">The level.</param>
/// <param name="CompetencyId">The competency.</param>
/// <param name="RequiredScore">The required point of the competency's own scale.</param>
public sealed record CareerVersionRequirement(Guid LevelId, Guid CompetencyId, int RequiredScore);
