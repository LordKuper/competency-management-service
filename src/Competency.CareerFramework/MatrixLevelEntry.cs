namespace Competency.CareerFramework;

/// <summary>
/// A career level of a matrix version, without its salary range.
/// </summary>
internal sealed record MatrixLevelEntry(
    Guid Id,
    string Name,
    string Code,
    bool IsManagerial,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? RoleDescription,
    CatalogStatus Status);
