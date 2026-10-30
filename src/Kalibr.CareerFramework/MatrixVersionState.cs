namespace Kalibr.CareerFramework;

/// <summary>
/// What one user may do with a matrix version at the moment: whose turn it is on review, whether it is the user's turn, whether a
/// returned version came back with the approver's changes, and the actions open to the user.
/// </summary>
internal sealed record MatrixVersionState(MatrixTurn? Turn, bool IsTurnHolder, bool ReturnedWithChanges, IReadOnlyList<MatrixAction> Actions);
