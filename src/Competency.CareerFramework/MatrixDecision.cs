namespace Competency.CareerFramework;

/// <summary>
/// One entry of a version's approval history: who did what, when, at which step, with which comment and variant.
/// Rows are only ever added.
/// </summary>
internal sealed class MatrixDecision
{
    public const int CommentMaxLength = 4000;

    public Guid VersionId { get; init; }

    /// <summary>
    /// The position of the entry in the version's history, from 1.
    /// </summary>
    public int Sequence { get; init; }

    public MatrixDecisionKind Kind { get; init; }

    public ApprovalStep? Step { get; init; }

    /// <summary>
    /// The account that acted; null for what the route did on its own, such as skipping a step.
    /// </summary>
    public Guid? ActorId { get; init; }

    public DateTimeOffset At { get; init; }

    public string? Comment { get; init; }

    /// <summary>
    /// The variant the decision produced, such as the submitted content or the approver's changes; null when it produced none.
    /// </summary>
    public int? VariantNumber { get; init; }
}
