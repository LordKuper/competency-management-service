namespace Kalibr.CareerFramework;

/// <summary>
/// The catalogue entries a matrix version refers to, as they stand: read live for a version in work, and frozen into a published
/// version at publication, so later renames and edits of the entries never change a published version. A track matrix names its
/// direction and track, a position matrix its position. The competencies include those of the shared blocks, each with its scale, and
/// the scales are those of the competencies; neither a matrix nor a shared block has a scale of its own.
/// </summary>
internal sealed record MatrixEntries(
    MatrixNamedEntry? Direction,
    MatrixNamedEntry? Track,
    IReadOnlyList<MatrixLevelEntry> Levels,
    IReadOnlyList<MatrixCompetencyEntry> Competencies,
    MatrixNamedEntry? Position = null,
    IReadOnlyList<MatrixSharedBlockEntry>? SharedBlocks = null,
    IReadOnlyList<MatrixScaleEntry>? Scales = null)
{
    /// <summary>
    /// The shared blocks of the version with their compositions.
    /// </summary>
    public IReadOnlyList<MatrixSharedBlockEntry> SharedBlocks { get; init; } = SharedBlocks ?? [];

    /// <summary>
    /// The scales of the version's competencies with their points, one entry per scale.
    /// </summary>
    public IReadOnlyList<MatrixScaleEntry> Scales { get; init; } = Scales ?? [];

    /// <summary>
    /// The composition of each shared block, by block.
    /// </summary>
    /// <returns>The compositions.</returns>
    public IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> Compositions() => SharedBlocks.ToDictionary(block => block.Id, block => block.CompetencyIds);

    /// <summary>
    /// The lowest score of each competency's scale, by competency; a competency whose scale is not listed or has no points has none.
    /// </summary>
    /// <returns>The lowest scores.</returns>
    public IReadOnlyDictionary<Guid, int> LowestScores()
    {
        var lowest = Scales.Where(scale => scale.Points.Count > 0).ToDictionary(scale => scale.Id, scale => scale.Points.Min(point => point.Score));
        return Competencies
            .Where(competency => competency.ScaleId is { } scaleId && lowest.ContainsKey(scaleId))
            .ToDictionary(competency => competency.Id, competency => lowest[competency.ScaleId!.Value]);
    }
}
