using System.ComponentModel;

namespace Competency.CareerFramework;

/// <summary>
/// A competency of a matrix version with the number of its version at the time and its own scale, which its scores are points of.
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
    Guid? ScaleId = null);
