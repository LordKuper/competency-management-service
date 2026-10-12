namespace Competency.CareerFramework;

/// <summary>
/// A competency as the API returns it. The version number counts the competency's versions from 1 and names its latest entry in the
/// version history; the version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// <c>CanEdit</c> and <c>CanDelete</c> say whether the signed-in user may change and delete the entry now, so a client offers only what the server allows.
/// </summary>
internal sealed record CompetencyResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    Guid? DirectionId,
    CompetencyType Type,
    Guid ScaleId,
    CatalogStatus Status,
    DateTimeOffset CreatedAt,
    int VersionNumber,
    int Version,
    bool CanEdit,
    bool CanDelete)
{
    public static CompetencyResponse From(CareerCompetency competency, CatalogViewer viewer) => new(
        competency.Id,
        competency.Name,
        competency.Description,
        competency.CategoryId,
        competency.DirectionId,
        competency.Type,
        competency.ScaleId,
        competency.Status,
        competency.CreatedAt,
        CompetencyVersion.NumberOf(competency),
        competency.Version,
        viewer.CanEdit(competency),
        viewer.CanDelete(competency));
}
