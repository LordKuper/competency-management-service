namespace Kalibr.CareerFramework;

/// <summary>
/// The completeness check of a matrix version without acting on it; empty problems mean the version may be handed on.
/// </summary>
internal sealed record MatrixCompletenessResponse(IReadOnlyList<MatrixProblem> Problems);
