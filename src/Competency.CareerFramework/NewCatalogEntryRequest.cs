namespace Competency.CareerFramework;

/// <summary>
/// A new direction or category: its name and the unit that keeps it, which never changes afterwards.
/// </summary>
internal record NewCatalogEntryRequest : CatalogNameRequest
{
    /// <summary>
    /// The unit that keeps the entry; null for a shared entry.
    /// </summary>
    public required Guid? OwnerOrgUnitId { get; init; }
}
