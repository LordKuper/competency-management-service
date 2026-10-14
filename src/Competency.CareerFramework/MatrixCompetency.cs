namespace Competency.CareerFramework;

/// <summary>
/// A competency of the version and the block it counts in: one of the version's own blocks, a shared block when the version's composition
/// is worked out, or null for the group of competencies outside blocks.
/// </summary>
internal sealed record MatrixCompetency(Guid CompetencyId, Guid? BlockId);
