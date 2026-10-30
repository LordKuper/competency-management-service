namespace Kalibr.CareerFramework;

/// <summary>
/// A decision in a version's history; the actor is null for what the route did on its own.
/// </summary>
internal sealed record MatrixDecisionResponse(
    int Sequence,
    MatrixDecisionKind Kind,
    ApprovalStep? Step,
    Guid? ActorId,
    string? ActorName,
    DateTimeOffset At,
    string? Comment,
    int? VariantNumber);
