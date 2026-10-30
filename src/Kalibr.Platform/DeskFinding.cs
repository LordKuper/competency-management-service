namespace Kalibr.Platform;

/// <summary>
/// One fact a module reports for the user's desk. Findings about the same object become one desk item, with the findings as its reasons.
/// </summary>
/// <param name="Kind">What the finding is; it decides how the desk absorbs, orders and groups it.</param>
/// <param name="Object">The object the finding is about; it decides which desk item the finding joins.</param>
/// <param name="IsAction">Whether the user can act on it and it counts as an expected action, rather than being for reference only.</param>
/// <param name="Subject">An object of another type the finding concerns, such as a unit head's account, through which a finding about that object absorbs this one.</param>
/// <param name="OccurredAt">When the event behind the finding happened or happens, for kinds that come from a dated event; null otherwise.</param>
/// <param name="Details">The values the finding's reason names.</param>
/// <param name="Target">What the item's link opens when it is not the item's own object, such as the version of a matrix.</param>
/// <param name="Invitation">The invitation the user can send again right from the desk.</param>
public sealed record DeskFinding(
    DeskKind Kind,
    DeskObject Object,
    bool IsAction,
    DeskObject? Subject = null,
    DateTimeOffset? OccurredAt = null,
    DeskDetails? Details = null,
    DeskObject? Target = null,
    DeskInvitation? Invitation = null);
