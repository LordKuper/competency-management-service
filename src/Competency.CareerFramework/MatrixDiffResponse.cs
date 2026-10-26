namespace Competency.CareerFramework;

/// <summary>
/// What a document changed against the one before it, or against an empty document when there is none: both documents, with the
/// compositions of their shared blocks written into their competencies, and the items that differ.
/// </summary>
internal sealed record MatrixDiffResponse(int Number, int? PreviousNumber, MatrixContent Before, MatrixContent After, IReadOnlyList<MatrixChange> Changes);
