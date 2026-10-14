namespace Competency.CareerFramework;

/// <summary>
/// A competency of a matrix version with the name of its category, the number of its version at the time and the scale its scores are
/// points of: its shared block's for a competency of a block, its own otherwise.
/// </summary>
internal sealed record MatrixCompetencyEntry(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    Guid? DirectionId,
    CompetencyType Type,
    int VersionNumber,
    CatalogStatus Status,
    Guid? ScaleId = null);
