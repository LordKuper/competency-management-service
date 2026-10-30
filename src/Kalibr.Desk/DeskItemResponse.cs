using Kalibr.Platform;

namespace Kalibr.Desk;

/// <summary>
/// One object on the desk with every reason it is there, the most important first.
/// </summary>
/// <param name="Object">The object.</param>
/// <param name="Group">The group of the first reason.</param>
/// <param name="IsReference">Whether the item is for reference only and does not count as an expected action.</param>
/// <param name="Reasons">The reasons, the most important first; the first decides where the item's link leads.</param>
/// <param name="Target">What the item's link opens when it is not the item's own object.</param>
/// <param name="Invitation">The invitation the user can send again right from the item.</param>
internal sealed record DeskItemResponse(
    DeskObject Object,
    DeskGroup Group,
    bool IsReference,
    IReadOnlyList<DeskReasonResponse> Reasons,
    DeskObject? Target = null,
    DeskInvitation? Invitation = null);
