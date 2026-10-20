using System.ComponentModel;

namespace Competency.CareerFramework;

/// <summary>
/// What a level requires of a competency: a required score, a point of the competency's own scale; for a competency critical in the
/// version it is also the threshold. The other fields are kept for clients of the contract that had them, never read and never stored.
/// </summary>
internal sealed record MatrixRequirement(
    Guid LevelId,
    Guid CompetencyId,
    int? RequiredScore,
    [property: Description(MatrixRequirement.Deprecated)] decimal? Weight = null,
    [property: Description(MatrixRequirement.Deprecated)] bool? IsMandatory = null,
    [property: Description(MatrixRequirement.Deprecated)] string? ExpectedBehavior = null,
    [property: Description(MatrixRequirement.Deprecated)] string? EvidenceExamples = null,
    [property: Description(MatrixRequirement.Deprecated)] DateOnly? ValidFrom = null,
    [property: Description(MatrixRequirement.DeprecatedType)] CompetencyType? Type = null)
{
    /// <summary>
    /// What the contract says of the fields of a requirement other than its score, which requirements no longer have.
    /// </summary>
    public const string Deprecated =
        "Deprecated, removed in the next major version: a requirement is a required score alone; ignored in requests, always null in responses.";

    /// <summary>
    /// What the contract says of the type of a requirement, which requirements no longer have.
    /// </summary>
    public const string DeprecatedType =
        "Deprecated, removed in the next major version: a competency's type is set once per version in competencies[].type or sharedBlocks[].competencyTypes; ignored in requests, always null in responses.";
}
