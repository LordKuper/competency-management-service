namespace Kalibr.Desk;

/// <summary>
/// One non-empty desk group with its first items.
/// </summary>
/// <param name="Group">The group.</param>
/// <param name="IsReference">Whether the group's items are for reference only.</param>
/// <param name="Total">The number of all items of the group.</param>
/// <param name="Items">The group's first items.</param>
internal sealed record DeskGroupResponse(DeskGroup Group, bool IsReference, int Total, IReadOnlyList<DeskItemResponse> Items);
