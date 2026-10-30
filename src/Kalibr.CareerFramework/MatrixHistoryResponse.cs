namespace Kalibr.CareerFramework;

/// <summary>
/// The approval history of a version: its variants, without their content, and its decisions, oldest first.
/// </summary>
internal sealed record MatrixHistoryResponse(IReadOnlyList<MatrixVariantSummaryResponse> Variants, IReadOnlyList<MatrixDecisionResponse> Decisions);
