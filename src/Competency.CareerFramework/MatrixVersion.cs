using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// One version of a competency matrix. The content is one document on the row, so any edit changes the row's version and is guarded
/// by <c>If-Match</c>. The document is left out of the journal; the journal records the status changes and explicit transition events.
/// </summary>
[Audited]
internal sealed class MatrixVersion : EntityBase
{
    [Audited]
    public Guid MatrixId { get; init; }

    public CompetencyMatrix Matrix { get; private set; } = null!;

    /// <summary>
    /// The version number within the matrix, from 1.
    /// </summary>
    [Audited]
    public int Number { get; init; }

    [Audited]
    public MatrixVersionStatus Status { get; set; }

    /// <summary>
    /// The step the version waits at while on review, or the step that returned it; null otherwise.
    /// </summary>
    [Audited]
    public ApprovalStep? Step { get; set; }

    /// <summary>
    /// The one scale of the version; every competency of the version is on it.
    /// </summary>
    [Audited]
    public Guid ScaleId { get; init; }

    /// <summary>
    /// The account that created the version.
    /// </summary>
    [Audited]
    public Guid AuthorId { get; init; }

    public MatrixContent Content { get; set; } = MatrixContent.Empty;

    /// <summary>
    /// The entries the version refers to as they stood at publication; null until the version is published.
    /// </summary>
    public MatrixEntries? Snapshot { get; set; }

    [Audited]
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// Whether the version counts as the one in work of its matrix, of which there is at most one.
    /// </summary>
    public bool IsInWork => Status is MatrixVersionStatus.Draft or MatrixVersionStatus.OnReview or MatrixVersionStatus.Returned;

    /// <summary>
    /// Whether the version is readable by every signed-in user.
    /// </summary>
    public bool IsPublic => Status is MatrixVersionStatus.Published or MatrixVersionStatus.Archived;
}
