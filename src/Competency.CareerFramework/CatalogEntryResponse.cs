namespace Competency.CareerFramework;

/// <summary>
/// A direction or competency category as the API returns it. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record CatalogEntryResponse(Guid Id, string Name, CatalogStatus Status, Guid? OwnerOrgUnitId, string? OwnerOrgUnitName, int Version)
{
    public static CatalogEntryResponse From(CatalogEntry entry, string? ownerOrgUnitName) =>
        new(entry.Id, entry.Name, entry.Status, entry.OwnerOrgUnitId, ownerOrgUnitName, entry.Version);
}
