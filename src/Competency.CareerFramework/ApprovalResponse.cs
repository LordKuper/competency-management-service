namespace Competency.CareerFramework;

/// <summary>
/// A version on review waiting for the signed-in user, with the step they act at and when it was last submitted.
/// </summary>
internal sealed record ApprovalResponse(
    Guid VersionId,
    Guid MatrixId,
    int Number,
    Guid TrackId,
    string TrackName,
    Guid OrgUnitId,
    string OrgUnitName,
    ApprovalStep Step,
    DateTimeOffset? SubmittedAt);
