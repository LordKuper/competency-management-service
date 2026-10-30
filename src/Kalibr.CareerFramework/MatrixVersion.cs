using Kalibr.Platform;

namespace Kalibr.CareerFramework;

/// <summary>
/// One version of a competency matrix. The content is one document on the row, so any edit changes the row's version and is guarded
/// by <c>If-Match</c>. The document is left out of the journal; the journal records the status changes and explicit transition events.
/// </summary>
[Audited]
internal sealed class MatrixVersion : EntityBase
{
    /// <summary>
    /// The first builder step a version can be saved at; the step before it chooses the track or the position, before the version exists.
    /// </summary>
    public const int FirstBuilderStep = 2;

    public const int LastBuilderStep = 6;

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
    /// The matrix builder step the version was last saved at, so the builder resumes there; null for a version made outside the builder.
    /// Left out of the journal, because it records where a user stopped rather than anything about the matrix.
    /// </summary>
    public short? BuilderStep { get; set; }

    /// <summary>
    /// Whether the version counts as the one in work of its matrix, of which there is at most one.
    /// </summary>
    public bool IsInWork => Status is MatrixVersionStatus.Draft or MatrixVersionStatus.OnReview or MatrixVersionStatus.Returned;

    /// <summary>
    /// Whether the version is readable by every signed-in user.
    /// </summary>
    public bool IsPublic => Status is MatrixVersionStatus.Published or MatrixVersionStatus.Archived;

    /// <summary>
    /// The compositions of the shared blocks the version counts with: frozen into the snapshot of a published or archived version, and
    /// the blocks as they stand now otherwise, so a version in work sees a block's change at once.
    /// </summary>
    /// <param name="current">The current composition of each block, covering at least the version's blocks unless it has a snapshot.</param>
    /// <returns>The compositions by block.</returns>
    public IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> CompositionsOf(IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> current) =>
        Snapshot?.Compositions() ?? current;
}
