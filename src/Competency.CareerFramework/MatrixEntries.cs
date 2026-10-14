using System.ComponentModel;

namespace Competency.CareerFramework;

/// <summary>
/// The catalogue entries a matrix version refers to, as they stand: read live for a version in work, and frozen into a published
/// version at publication, so later renames and edits of the entries never change a published version. A track matrix names its
/// direction and track, a position matrix its position. The competencies include those of the shared blocks, each with its scale, and
/// the scales are those of the competencies and blocks; a matrix has no scale of its own.
/// </summary>
internal sealed record MatrixEntries(
    MatrixNamedEntry? Direction,
    MatrixNamedEntry? Track,
    [property: Description(MatrixEntries.DeprecatedScale)] MatrixScaleEntry? Scale,
    IReadOnlyList<MatrixLevelEntry> Levels,
    IReadOnlyList<MatrixCompetencyEntry> Competencies,
    MatrixNamedEntry? Position = null,
    IReadOnlyList<MatrixSharedBlockEntry>? SharedBlocks = null,
    IReadOnlyList<MatrixScaleEntry>? Scales = null)
{
    /// <summary>
    /// What the contract says of the scale of a version, which matrices no longer have.
    /// </summary>
    public const string DeprecatedScale =
        "Deprecated, removed in the next major version: the one scale of a version published before matrices lost their scale; null otherwise. Use competencies[].scaleId and scales.";

    /// <summary>
    /// The shared blocks of the version with their compositions; empty in a snapshot taken before shared blocks existed.
    /// </summary>
    public IReadOnlyList<MatrixSharedBlockEntry> SharedBlocks { get; init; } = SharedBlocks ?? [];

    /// <summary>
    /// The scales of the version's competencies and shared blocks with their points, one entry per scale.
    /// </summary>
    public IReadOnlyList<MatrixScaleEntry> Scales { get; init; } = Scales ?? [];

    /// <summary>
    /// The composition of each shared block, by block.
    /// </summary>
    /// <returns>The compositions.</returns>
    public IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> Compositions() => SharedBlocks.ToDictionary(block => block.Id, block => block.CompetencyIds);
}
