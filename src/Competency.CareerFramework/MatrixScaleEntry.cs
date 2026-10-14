namespace Competency.CareerFramework;

/// <summary>
/// A scale of a matrix version's competencies or shared blocks with its points.
/// </summary>
internal sealed record MatrixScaleEntry(Guid Id, string Name, IReadOnlyList<ScalePoint> Points, CatalogStatus Status);
