namespace Competency.CareerFramework;

/// <summary>
/// A direction or competency category as the API returns it. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// <c>CanEdit</c> and <c>CanDelete</c> say whether the signed-in user may change and delete the entry now, so a client offers only what the server allows.
/// </summary>
internal sealed record CatalogEntryResponse(Guid Id, string Name, CatalogStatus Status, int Version, bool CanEdit, bool CanDelete)
{
    public static CatalogEntryResponse From(CatalogEntry entry, CatalogViewer viewer) =>
        new(entry.Id, entry.Name, entry.Status, entry.Version, viewer.CanEdit(entry), viewer.CanDelete(entry));
}
