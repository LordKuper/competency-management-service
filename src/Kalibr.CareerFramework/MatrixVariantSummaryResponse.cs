namespace Kalibr.CareerFramework;

/// <summary>
/// A variant in a version's history.
/// </summary>
internal sealed record MatrixVariantSummaryResponse(int Number, MatrixVariantKind Kind, Guid CreatedById, string? CreatedByName, DateTimeOffset CreatedAt);
