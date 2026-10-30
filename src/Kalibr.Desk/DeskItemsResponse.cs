namespace Competency.Desk;

/// <summary>
/// The first items of the chosen desk groups.
/// </summary>
/// <param name="Items">The items in desk order, at most as many as asked for and never more than the desk's ceiling.</param>
/// <param name="Total">The number of all items of the chosen groups.</param>
internal sealed record DeskItemsResponse(IReadOnlyList<DeskItemResponse> Items, int Total);
