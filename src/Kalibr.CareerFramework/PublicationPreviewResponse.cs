namespace Kalibr.CareerFramework;

/// <summary>
/// What approving or reactivating a catalogue entry would publish now, and what keeps it from being published.
/// </summary>
/// <param name="Entries">The drafts that become active with the entry the user can read.</param>
/// <param name="HiddenEntryCount">The number of drafts that become active with the entry the user cannot read, so not listed.</param>
/// <param name="Blockers">At most five entries in the way the user can read.</param>
/// <param name="MoreCount">The number of entries in the way not listed, the ones the user cannot read included.</param>
/// <param name="CascadeTag">The tag the action sends back in <c>?cascade=</c> to publish exactly this list.</param>
internal sealed record PublicationPreviewResponse(
    IReadOnlyList<PublicationEntry> Entries,
    int HiddenEntryCount,
    IReadOnlyList<PublicationBlocker> Blockers,
    int MoreCount,
    string CascadeTag);
