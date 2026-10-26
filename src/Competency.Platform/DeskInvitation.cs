namespace Competency.Platform;

/// <summary>
/// An invitation the user can send again right from the desk, with the account version the request sends back in <c>If-Match</c>.
/// </summary>
/// <param name="AccountId">The invited account.</param>
/// <param name="Version">The account's version.</param>
public sealed record DeskInvitation(Guid AccountId, int Version);
