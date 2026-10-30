namespace Kalibr.CareerFramework;

/// <summary>
/// One entry of a competency's version history: the competency as it stood after the change, and who made the change and when.
/// The author is named by their employee's full name, or else by their e-mail; null for the entries the database starts with.
/// </summary>
internal sealed record CompetencyVersionResponse(
    int Number,
    string Name,
    string? Description,
    Guid? DirectionId,
    Guid ScaleId,
    CatalogStatus Status,
    Guid? ChangedById,
    string? ChangedByName,
    DateTimeOffset ChangedAt,
    string? EvidenceExamples = null);
