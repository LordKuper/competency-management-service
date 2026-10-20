using System.ComponentModel;

namespace Competency.CareerFramework;

/// <summary>
/// One entry of a competency's version history: the competency as it stood after the change, and who made the change and when.
/// The author is named by their employee's full name, or else by their e-mail; null for the entries the database starts with.
/// </summary>
internal sealed record CompetencyVersionResponse(
    int Number,
    string Name,
    string? Description,
    [property: Description(CompetencyResponse.DeprecatedCategory)] Guid? CategoryId,
    Guid? DirectionId,
    CompetencyType Type,
    Guid ScaleId,
    CatalogStatus Status,
    Guid? ChangedById,
    string? ChangedByName,
    DateTimeOffset ChangedAt);
