namespace Competency.Promotion;

/// <summary>
/// A position whose holders cannot be named as raters.
/// </summary>
/// <param name="Id">The position.</param>
/// <param name="Name">The position name.</param>
/// <param name="EmployeeCount">The number of working employees who hold the position now.</param>
internal sealed record ExcludedPositionResponse(Guid Id, string Name, int EmployeeCount);
