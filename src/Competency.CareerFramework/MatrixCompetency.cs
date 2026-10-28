namespace Competency.CareerFramework;

/// <summary>
/// A competency of the version, the block it counts in and its type in the version. The block is one of the version's own blocks, a
/// shared block when the version's composition is worked out, or null for the group of competencies outside blocks. The type may be
/// left out of a request, which keeps the type the document already has or else makes the competency an additional one.
/// </summary>
internal sealed record MatrixCompetency(Guid CompetencyId, Guid? BlockId, CompetencyType? Type = null);
