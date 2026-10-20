using System.ComponentModel;

namespace Competency.CareerFramework;

/// <summary>
/// What a level requires of a competency. The required score is a point of the competency's own scale; a competency critical in the
/// version must set it. The type is the competency's in the version, not the requirement's.
/// </summary>
internal sealed record MatrixRequirement(
    Guid LevelId,
    Guid CompetencyId,
    int? RequiredScore,
    decimal Weight,
    bool IsMandatory,
    string? ExpectedBehavior,
    string? EvidenceExamples,
    DateOnly? ValidFrom,
    [property: Description(MatrixRequirement.DeprecatedType)] CompetencyType? Type = null)
{
    /// <summary>
    /// What the contract says of the type of a requirement, which requirements no longer have.
    /// </summary>
    public const string DeprecatedType =
        "Deprecated, removed in the next major version: a competency's type is set once per version in competencies[].type or sharedBlocks[].competencyTypes; ignored in requests, always null in responses.";
}
