namespace Competency.CareerFramework;

/// <summary>
/// What a variant changed against the variant before it, or against an empty document for the first variant: both documents and the
/// items that differ.
/// </summary>
internal sealed record MatrixDiffResponse(int Number, int? PreviousNumber, MatrixContent Before, MatrixContent After, IReadOnlyList<MatrixChange> Changes);
