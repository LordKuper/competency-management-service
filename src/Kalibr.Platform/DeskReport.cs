namespace Kalibr.Platform;

/// <summary>
/// What one desk source reports for the signed-in user in one call.
/// </summary>
/// <param name="Findings">The findings, at most one of each kind per object and subject; empty when the user's roles give the source nothing to report.</param>
/// <param name="Prerequisites">The scenario prerequisites the source's data meets now; empty when it knows of none that hold.</param>
public sealed record DeskReport(IReadOnlyList<DeskFinding> Findings, IReadOnlyCollection<DeskPrerequisite> Prerequisites);
