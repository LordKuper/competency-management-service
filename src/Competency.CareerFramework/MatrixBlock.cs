namespace Competency.CareerFramework;

/// <summary>
/// A block of a version's own, kept only in the contract for the clients that had them: a version has no blocks of its own, its groups
/// are shared blocks and the competencies outside blocks.
/// </summary>
internal sealed record MatrixBlock(Guid Id, string Name, bool IsMandatory);
