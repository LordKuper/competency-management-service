namespace Competency.CareerFramework;

/// <summary>
/// Whose turn it is on a version on review: the step that applies at the moment and its approver, if anyone can act at it.
/// </summary>
internal sealed record MatrixTurn(ApprovalStep Step, Guid? ApproverId);
