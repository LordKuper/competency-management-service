namespace Competency.CareerFramework;

/// <summary>
/// The catalogue entries a matrix version refers to, as they stand: read live for a version in work, and frozen into a published
/// version at publication, so later renames and edits of the entries never change a published version. A track matrix names its
/// direction and track, a position matrix its position. The competencies include those of the shared blocks.
/// </summary>
internal sealed record MatrixEntries(
    MatrixNamedEntry? Direction,
    MatrixNamedEntry? Track,
    MatrixScaleEntry Scale,
    IReadOnlyList<MatrixLevelEntry> Levels,
    IReadOnlyList<MatrixCompetencyEntry> Competencies,
    MatrixNamedEntry? Position = null,
    IReadOnlyList<MatrixSharedBlockEntry>? SharedBlocks = null)
{
    /// <summary>
    /// The shared blocks of the version with their compositions; empty in a snapshot taken before shared blocks existed.
    /// </summary>
    public IReadOnlyList<MatrixSharedBlockEntry> SharedBlocks { get; init; } = SharedBlocks ?? [];

    /// <summary>
    /// The composition of each shared block, by block.
    /// </summary>
    /// <returns>The compositions.</returns>
    public IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> Compositions() => SharedBlocks.ToDictionary(block => block.Id, block => block.CompetencyIds);
}
