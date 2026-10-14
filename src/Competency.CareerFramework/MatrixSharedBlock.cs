namespace Competency.CareerFramework;

/// <summary>
/// A shared block a version includes by reference, whose composition the block itself keeps; a mandatory block may set its own readiness threshold.
/// </summary>
internal sealed record MatrixSharedBlock(Guid BlockId, bool IsMandatory);
