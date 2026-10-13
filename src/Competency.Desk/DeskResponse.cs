namespace Competency.Desk;

/// <summary>
/// The signed-in user's desk.
/// </summary>
/// <param name="ActionCount">The number of items that are expected actions, for the counter.</param>
/// <param name="Groups">The non-empty groups in desk order.</param>
internal sealed record DeskResponse(int ActionCount, IReadOnlyList<DeskGroupResponse> Groups);
