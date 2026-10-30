namespace Kalibr.CareerFramework;

/// <summary>
/// An active catalogue entry nothing uses. The version equals the entry's <c>ETag</c>, so deleting or deactivating it from the list
/// sends back the tag the user saw.
/// </summary>
internal sealed record UnusedEntryResponse(CatalogEntryKind Kind, Guid Id, string Name, int Version);
