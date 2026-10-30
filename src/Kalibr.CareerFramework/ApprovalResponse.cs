namespace Competency.CareerFramework;

/// <summary>
/// A version on review waiting for the signed-in user, with the step they act at and when it was last submitted. The matrix is named
/// by its track or, for a position matrix, by its position; the fields of the other kind are null.
/// </summary>
internal sealed record ApprovalResponse(
    Guid VersionId,
    Guid MatrixId,
    int Number,
    Guid? TrackId,
    string? TrackName,
    Guid OrgUnitId,
    string OrgUnitName,
    ApprovalStep Step,
    DateTimeOffset? SubmittedAt,
    Guid? PositionId = null,
    string? PositionName = null);
