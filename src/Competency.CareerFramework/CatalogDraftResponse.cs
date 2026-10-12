namespace Competency.CareerFramework;

/// <summary>
/// A draft entry awaiting approval, with its unit for a track. The author is named by their employee's full name, or else by their e-mail.
/// The version equals the entry's <c>ETag</c>, so approving it from the list sends back the tag the user saw.
/// </summary>
internal sealed record CatalogDraftResponse(
    CatalogEntryKind Kind,
    Guid Id,
    string Name,
    Guid? OwnerOrgUnitId,
    string? OwnerOrgUnitName,
    Guid? AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt,
    int Version);
