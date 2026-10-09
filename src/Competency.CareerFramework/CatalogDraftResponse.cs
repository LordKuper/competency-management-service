namespace Competency.CareerFramework;

/// <summary>
/// A draft entry awaiting approval. The author is named by their employee's full name, or else by their e-mail.
/// </summary>
internal sealed record CatalogDraftResponse(
    CatalogEntryKind Kind,
    Guid Id,
    string Name,
    Guid? OwnerOrgUnitId,
    string? OwnerOrgUnitName,
    Guid? AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt);
