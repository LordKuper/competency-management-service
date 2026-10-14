namespace Competency.CareerFramework;

/// <summary>
/// A direction, track or position a matrix version is built for; a position is active or inactive.
/// </summary>
internal sealed record MatrixNamedEntry(Guid Id, string Name, CatalogStatus Status);
