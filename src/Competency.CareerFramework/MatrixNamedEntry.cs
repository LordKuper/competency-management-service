namespace Competency.CareerFramework;

/// <summary>
/// A direction or track a matrix version is built for.
/// </summary>
internal sealed record MatrixNamedEntry(Guid Id, string Name, CatalogStatus Status);
