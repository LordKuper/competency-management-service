namespace Competency.CareerFramework;

/// <summary>
/// The scale of a matrix version with its points.
/// </summary>
internal sealed record MatrixScaleEntry(Guid Id, string Name, IReadOnlyList<ScalePoint> Points, CatalogStatus Status);
