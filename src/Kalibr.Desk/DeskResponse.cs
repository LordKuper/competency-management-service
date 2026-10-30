using Kalibr.Platform;

namespace Kalibr.Desk;

/// <summary>
/// The signed-in user's desk.
/// </summary>
/// <param name="ActionCount">The number of items that are expected actions, for the counter.</param>
/// <param name="Groups">The non-empty groups in desk order.</param>
/// <param name="Prerequisites">The scenario prerequisites that hold now, each once, in declaration order; the client offers a first step only when its prerequisites are here.</param>
internal sealed record DeskResponse(int ActionCount, IReadOnlyList<DeskGroupResponse> Groups, IReadOnlyList<DeskPrerequisite> Prerequisites);
