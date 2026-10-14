namespace Competency.CareerFramework;

/// <summary>
/// A shared block of a matrix version with its scale and its composition.
/// </summary>
internal sealed record MatrixSharedBlockEntry(Guid Id, string Name, CatalogStatus Status, MatrixScaleEntry Scale, IReadOnlyList<Guid> CompetencyIds);
