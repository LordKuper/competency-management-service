namespace Competency.CareerFramework;

/// <summary>
/// What a level requires of a competency. The required score is a point of the version's scale; a critical requirement must set it.
/// </summary>
internal sealed record MatrixRequirement(
    Guid LevelId,
    Guid CompetencyId,
    int? RequiredScore,
    CompetencyType Type,
    decimal Weight,
    bool IsMandatory,
    string? ExpectedBehavior,
    string? EvidenceExamples,
    DateOnly? ValidFrom);
