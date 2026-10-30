namespace Competency.CareerFramework;

/// <summary>
/// A version in a matrix's list of versions.
/// </summary>
internal sealed record MatrixVersionSummaryResponse(
    Guid Id,
    int Number,
    MatrixVersionStatus Status,
    Guid AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);
