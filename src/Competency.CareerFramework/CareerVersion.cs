namespace Competency.CareerFramework;

/// <summary>
/// A published or archived track matrix version with the composition of its blocks, scales and coefficients frozen at publication, so a
/// rating computed from it never changes when the catalogue does.
/// </summary>
/// <param name="Levels">The levels of the version, the most junior first.</param>
/// <param name="Competencies">The competencies the version counts, each once.</param>
/// <param name="Requirements">The required score of every level and competency.</param>
/// <param name="BlockWeights">The coefficients of the groups of competencies by level.</param>
public sealed record CareerVersion(
    IReadOnlyList<CareerVersionLevel> Levels,
    IReadOnlyList<CareerVersionCompetency> Competencies,
    IReadOnlyList<CareerVersionRequirement> Requirements,
    IReadOnlyList<CareerVersionBlockWeight> BlockWeights);
