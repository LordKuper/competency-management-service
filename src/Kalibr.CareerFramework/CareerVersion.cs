namespace Kalibr.CareerFramework;

/// <summary>
/// A published or archived track matrix version with the composition of its blocks, scales and coefficients frozen at publication, so a
/// rating computed from it never changes when the catalogue does.
/// </summary>
/// <param name="Levels">The levels of the version, the most junior first.</param>
/// <param name="Competencies">The competencies the version counts, each once.</param>
/// <param name="Requirements">The required score of every level and competency.</param>
/// <param name="BlockWeights">The coefficients of the groups of competencies by level.</param>
/// <param name="Blocks">The shared blocks the counted competencies belong to, in the order the version includes them.</param>
/// <param name="CompetencyWeights">The weights of competencies in groups that have their own; empty when every group weighs its competencies equally.</param>
public sealed record CareerVersion(
    IReadOnlyList<CareerVersionLevel> Levels,
    IReadOnlyList<CareerVersionCompetency> Competencies,
    IReadOnlyList<CareerVersionRequirement> Requirements,
    IReadOnlyList<CareerVersionBlockWeight> BlockWeights,
    IReadOnlyList<CareerVersionBlock> Blocks,
    IReadOnlyList<CareerVersionCompetencyWeight>? CompetencyWeights = null)
{
    /// <summary>
    /// The weights of competencies in groups that have their own; empty when sent without them.
    /// </summary>
    public IReadOnlyList<CareerVersionCompetencyWeight> CompetencyWeights { get; init; } = CompetencyWeights ?? [];
}
