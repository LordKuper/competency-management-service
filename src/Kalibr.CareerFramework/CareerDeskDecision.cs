namespace Competency.CareerFramework;

/// <summary>
/// What the desk needs of a decision on a version in work: when it was taken and what the approver sent back with it.
/// </summary>
/// <param name="VersionId">The version.</param>
/// <param name="Sequence">The decision's place in the version's history.</param>
/// <param name="At">When the decision was taken.</param>
/// <param name="HasComment">Whether the decision carries a comment.</param>
/// <param name="HasChanges">Whether the decision produced a variant, such as the approver's changes.</param>
internal sealed record CareerDeskDecision(Guid VersionId, int Sequence, DateTimeOffset At, bool HasComment, bool HasChanges);
