namespace Competency.CareerFramework;

/// <summary>
/// A scale as the API returns it, with its points in ascending order of score. The version equals the <c>ETag</c> a change returns
/// and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record CompetencyScaleResponse(
    Guid Id,
    string Name,
    IReadOnlyList<ScalePoint> Points,
    CatalogStatus Status,
    Guid? OwnerOrgUnitId,
    string? OwnerOrgUnitName,
    int Version)
{
    public static CompetencyScaleResponse From(CompetencyScale scale, string? ownerOrgUnitName) =>
        new(scale.Id, scale.Name, scale.Points, scale.Status, scale.OwnerOrgUnitId, ownerOrgUnitName, scale.Version);
}
