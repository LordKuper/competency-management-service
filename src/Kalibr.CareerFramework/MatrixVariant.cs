namespace Competency.CareerFramework;

/// <summary>
/// A copy of a version's content as one participant handed it on: submitted by the unit head or returned with changes by an approver.
/// Rows are only ever added, and deleted only with a matrix that was never published; changes between variants are shown by comparing the copies.
/// </summary>
internal sealed class MatrixVariant
{
    public Guid VersionId { get; init; }

    /// <summary>
    /// The variant number within the version, from 1.
    /// </summary>
    public int Number { get; init; }

    public MatrixVariantKind Kind { get; init; }

    public required MatrixContent Content { get; init; }

    public Guid CreatedById { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
