using Microsoft.AspNetCore.Http.HttpResults;

namespace Competency.CareerFramework;

/// <summary>
/// One publication as worked out under the current data: the drafts that become active with the root, what keeps it from happening,
/// and the tag of the list. A refusal and a preview list the same entries in the way, so what the user saw is what was refused.
/// </summary>
/// <param name="Entries">The tracked drafts published with the root, in the order the list shows them, already set active in memory.</param>
/// <param name="Blockers">Every entry in the way with the reason; empty when the publication may go ahead.</param>
/// <param name="Tag">The hash of the root and of every draft with its version.</param>
internal sealed record PublicationPlan(
    IReadOnlyList<CatalogEntry> Entries,
    IReadOnlyList<(CatalogEntry Entry, PublicationBlocker Blocker)> Blockers,
    string Tag)
{
    /// <summary>
    /// Whether something keeps the publication from going ahead.
    /// </summary>
    public bool IsRefused => Blockers.Count > 0;

    /// <summary>
    /// Whether the tag the user confirmed is the tag of this plan.
    /// </summary>
    /// <param name="cascade">The tag from <c>?cascade=</c>; null when the request sent none.</param>
    /// <returns>True when the user confirmed exactly this list.</returns>
    public bool IsConfirmedBy(string? cascade) => string.Equals(Tag, cascade, StringComparison.Ordinal);

    /// <summary>
    /// The 409 refusal, listing the entries in the way the reader can read.
    /// </summary>
    /// <param name="detail">The heading of the refusal, addressed to the user.</param>
    /// <param name="reader">The rights of the user who asked.</param>
    /// <returns>A 409 response.</returns>
    public ProblemHttpResult Refuse(string detail, CatalogViewer reader)
    {
        var visible = Visible(reader);
        return PublicationRefusal.Refuse(detail, visible, Blockers.Count - visible.Count);
    }

    /// <summary>
    /// The preview of a catalogue entry's publication for the reader.
    /// </summary>
    /// <param name="reader">The rights of the user who asked.</param>
    /// <returns>The preview.</returns>
    public PublicationPreviewResponse Describe(CatalogViewer reader)
    {
        var visible = Visible(reader);
        return new PublicationPreviewResponse(
            DescribeEntries(),
            [.. visible.Take(PublicationRefusal.ShownLimit)],
            Blockers.Count - Math.Min(visible.Count, PublicationRefusal.ShownLimit),
            Tag);
    }

    /// <summary>
    /// The preview of a matrix version's publication for the reader.
    /// </summary>
    /// <param name="publishingActions">The reader's actions that would publish the version.</param>
    /// <param name="reader">The rights of the user who asked.</param>
    /// <returns>The preview.</returns>
    public MatrixPublicationResponse Describe(IReadOnlyList<MatrixAction> publishingActions, CatalogViewer reader)
    {
        var preview = Describe(reader);
        return new MatrixPublicationResponse(publishingActions, preview.Entries, preview.Blockers, preview.MoreCount, preview.CascadeTag);
    }

    private List<PublicationEntry> DescribeEntries() =>
        [.. Entries.Select(entry => new PublicationEntry(CatalogPublication.KindOf(entry), entry.Id, entry.Name, CatalogPublication.ContextOf(entry)))];

    private List<PublicationBlocker> Visible(CatalogViewer reader) =>
        [.. Blockers.Where(blocker => reader.CanSee(blocker.Entry)).Select(blocker => blocker.Blocker)];
}
