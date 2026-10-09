namespace Competency.CareerFramework;

/// <summary>
/// A competency as the API returns it. The version number counts the competency's versions from 1 and names its latest entry in the
/// version history; the version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
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
    Guid? OwnerOrgUnitId,
    string? OwnerOrgUnitName,
    DateTimeOffset CreatedAt,
    int VersionNumber,
    int Version)
{
    public static CompetencyResponse From(CareerCompetency competency, string? ownerOrgUnitName) => new(
        competency.Id,
        competency.Name,
        competency.Description,
        competency.CategoryId,
        competency.DirectionId,
        competency.Type,
        competency.ScaleId,
        competency.Status,
        competency.OwnerOrgUnitId,
        ownerOrgUnitName,
        competency.CreatedAt,
        CompetencyVersion.NumberOf(competency),
        competency.Version);
}
