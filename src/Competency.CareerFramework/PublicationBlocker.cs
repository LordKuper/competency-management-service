namespace Competency.CareerFramework;

/// <summary>
/// One entry that keeps a career entry from being active, as a refusal lists it.
/// </summary>
/// <param name="Kind">What the entry is.</param>
/// <param name="Id">The entry.</param>
/// <param name="Name">The entry's name.</param>
/// <param name="Reason">Why it is in the way.</param>
/// <param name="Context">Where the entry sits: a level's track name; null for a track.</param>
internal sealed record PublicationBlocker(PublicationBlockerKind Kind, Guid Id, string Name, PublicationBlockerReason Reason, string? Context);
