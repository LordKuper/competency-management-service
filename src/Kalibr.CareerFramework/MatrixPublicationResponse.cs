namespace Kalibr.CareerFramework;

/// <summary>
/// What publishing a matrix version would make active now, which of the user's actions would publish it, and what keeps it from being published.
/// </summary>
/// <param name="PublishingActions">The actions open to the user now that would publish the version; the client does not work them out itself.</param>
/// <param name="Entries">The drafts that become active with the version the user can read.</param>
/// <param name="HiddenEntryCount">The number of drafts that become active with the version the user cannot read, so not listed.</param>
/// <param name="Blockers">At most five entries in the way the user can read, judged by the right of the responsible HR of the matrix unit.</param>
/// <param name="MoreCount">The number of entries in the way not listed, the ones the user cannot read included.</param>
/// <param name="CascadeTag">The tag a publishing action sends back in <c>?cascade=</c> to publish exactly this list.</param>
internal sealed record MatrixPublicationResponse(
    IReadOnlyList<MatrixAction> PublishingActions,
    IReadOnlyList<PublicationEntry> Entries,
    int HiddenEntryCount,
    IReadOnlyList<PublicationBlocker> Blockers,
    int MoreCount,
    string CascadeTag);
