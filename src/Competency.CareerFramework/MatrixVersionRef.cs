namespace Competency.CareerFramework;

/// <summary>
/// A version named in a matrix response.
/// </summary>
internal sealed record MatrixVersionRef(Guid Id, int Number, MatrixVersionStatus Status);
