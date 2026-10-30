namespace Competency.CareerFramework;

/// <summary>
/// A version named in a matrix response. The version equals the version's <c>ETag</c>, so an action taken from the row sends back the tag the user saw.
/// </summary>
internal sealed record MatrixVersionRef(Guid Id, int Number, MatrixVersionStatus Status, int Version);
