namespace Kalibr.Promotion;

/// <summary>
/// How many applications of a list stand in a status.
/// </summary>
/// <param name="Status">The status.</param>
/// <param name="Count">The applications.</param>
internal sealed record ApplicationStatusCountResponse(ApplicationStatus Status, int Count);
