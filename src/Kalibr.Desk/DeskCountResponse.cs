namespace Kalibr.Desk;

/// <summary>
/// The number of expected actions on the signed-in user's desk.
/// </summary>
/// <param name="ActionCount">The number of desk items that are expected actions.</param>
internal sealed record DeskCountResponse(int ActionCount);
