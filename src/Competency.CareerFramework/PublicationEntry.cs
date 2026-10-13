namespace Competency.CareerFramework;

/// <summary>
/// One draft that becomes active together with the published entry or version.
/// </summary>
/// <param name="Kind">What the draft is.</param>
/// <param name="Id">The draft.</param>
/// <param name="Name">The draft's name.</param>
/// <param name="Context">Where the draft sits: a level's track name; null for the other kinds.</param>
internal sealed record PublicationEntry(PublicationEntryKind Kind, Guid Id, string Name, string? Context);
