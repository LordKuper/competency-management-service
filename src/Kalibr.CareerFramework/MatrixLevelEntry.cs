namespace Kalibr.CareerFramework;

/// <summary>
/// A career level of a matrix version.
/// </summary>
internal sealed record MatrixLevelEntry(
    Guid Id,
    string Name,
    CatalogStatus Status);
