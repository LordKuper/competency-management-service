using System.ComponentModel;

namespace Competency.CareerFramework;

/// <summary>
/// A competency of a matrix version with the number of its version at the time, its own scale, which its scores are points of, its type
/// in the version and its evidence examples; a snapshot taken before competencies had examples has none.
/// </summary>
internal sealed record MatrixCompetencyEntry(
    Guid Id,
    string Name,
    string? Description,
    [property: Description(CompetencyResponse.DeprecatedCategory)] Guid? CategoryId,
    [property: Description(CompetencyResponse.DeprecatedCategory)] string? CategoryName,
    Guid? DirectionId,
    CompetencyType Type,
    int VersionNumber,
    CatalogStatus Status,
    Guid? ScaleId = null,
    string? EvidenceExamples = null);
