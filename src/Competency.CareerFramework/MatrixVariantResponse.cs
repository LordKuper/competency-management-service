namespace Competency.CareerFramework;

/// <summary>
/// A variant with its content.
/// </summary>
internal sealed record MatrixVariantResponse(
    int Number,
    MatrixVariantKind Kind,
    Guid CreatedById,
    string? CreatedByName,
    DateTimeOffset CreatedAt,
    MatrixContent Content);
