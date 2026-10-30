namespace Kalibr.CareerFramework;

/// <summary>
/// A shared block of a matrix version with its composition; a block has no scale, each of its competencies has its own.
/// </summary>
internal sealed record MatrixSharedBlockEntry(Guid Id, string Name, CatalogStatus Status, IReadOnlyList<Guid> CompetencyIds);
