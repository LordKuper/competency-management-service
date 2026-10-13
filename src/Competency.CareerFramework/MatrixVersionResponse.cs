namespace Competency.CareerFramework;

/// <summary>
/// A matrix version as the API returns it. The entries are the published snapshot for a published or archived version, and read live
/// otherwise. The step is the one that applies now, or the one that returned the version. For a version in work, the route names the
/// superior approver, null when the step would be skipped, and the responsible HR, null when there is none. The actions are what the
/// signed-in user may do now. The builder step is where the matrix builder resumes, null for a version made outside it.
/// The version equals the <c>ETag</c> and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record MatrixVersionResponse(
    Guid Id,
    Guid MatrixId,
    int Number,
    MatrixVersionStatus Status,
    ApprovalStep? Step,
    Guid ScaleId,
    Guid AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    MatrixContent Content,
    MatrixEntries Entries,
    CareerPersonResponse? SuperiorApprover,
    CareerPersonResponse? Hr,
    bool ReturnedWithChanges,
    IReadOnlyList<MatrixAction> Actions,
    int? BuilderStep,
    int Version);
