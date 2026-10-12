namespace Competency.CareerFramework;

/// <summary>
/// A competency as the API returns it, with the names of its category, direction and scale; a general competency has no direction
/// and so no direction name. The version number counts the competency's versions from 1 and names its latest entry in the
/// version history; the version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// <c>CanEdit</c> and <c>CanDelete</c> say whether the signed-in user may change and delete the entry now, so a client offers only what the server allows.
/// </summary>
internal sealed record CompetencyResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    Guid? DirectionId,
    string? DirectionName,
    CompetencyType Type,
    Guid ScaleId,
    string ScaleName,
    CatalogStatus Status,
    DateTimeOffset CreatedAt,
    int VersionNumber,
    int Version,
    bool CanEdit,
    bool CanDelete)
{
    public static CompetencyResponse From(CareerCompetency competency, IReadOnlyDictionary<Guid, string> names, CatalogViewer viewer) => new(
        competency.Id,
        competency.Name,
        competency.Description,
        competency.CategoryId,
        names.GetValueOrDefault(competency.CategoryId, string.Empty),
        competency.DirectionId,
        competency.DirectionId is { } directionId ? names.GetValueOrDefault(directionId, string.Empty) : null,
        competency.Type,
        competency.ScaleId,
        names.GetValueOrDefault(competency.ScaleId, string.Empty),
        competency.Status,
        competency.CreatedAt,
        CompetencyVersion.NumberOf(competency),
        competency.Version,
        viewer.CanEdit(competency),
        viewer.CanDelete(competency));
}
