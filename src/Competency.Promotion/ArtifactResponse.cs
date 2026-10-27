namespace Competency.Promotion;

/// <summary>
/// The evidence a candidate gave for a competency.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="CompetencyName">The competency name as the bound version froze it.</param>
/// <param name="Text">What the candidate did, in words.</param>
/// <param name="Links">Addresses of documents in other systems.</param>
internal sealed record ArtifactResponse(Guid CompetencyId, string CompetencyName, string Text, IReadOnlyList<string> Links);
