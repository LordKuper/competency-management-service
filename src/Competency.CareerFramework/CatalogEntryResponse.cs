namespace Competency.CareerFramework;

/// <summary>
/// A direction or competency category as the API returns it. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record CatalogEntryResponse(Guid Id, string Name, CatalogStatus Status, int Version)
{
    public static CatalogEntryResponse From(CatalogEntry entry) => new(entry.Id, entry.Name, entry.Status, entry.Version);
}
