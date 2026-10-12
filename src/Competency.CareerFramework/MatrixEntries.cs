namespace Competency.CareerFramework;

/// <summary>
/// The catalogue entries a matrix version refers to, as they stand: read live for a version in work, and frozen into a published
/// version at publication, so later renames and edits of the entries never change a published version.
/// </summary>
internal sealed record MatrixEntries(
    MatrixNamedEntry Direction,
    MatrixNamedEntry Track,
    MatrixScaleEntry Scale,
    IReadOnlyList<MatrixLevelEntry> Levels,
    IReadOnlyList<MatrixCompetencyEntry> Competencies);
