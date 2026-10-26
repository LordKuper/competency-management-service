namespace Competency.CareerFramework;

/// <summary>
/// A competency of a matrix version with the number of its version at the time, its own scale, which its scores are points of, its type
/// in the version and its evidence examples.
/// </summary>
internal sealed record MatrixCompetencyEntry(
    Guid Id,
    string Name,
    string? Description,
    Guid? DirectionId,
    CompetencyType Type,
    int VersionNumber,
    CatalogStatus Status,
    Guid? ScaleId = null,
    string? EvidenceExamples = null);
