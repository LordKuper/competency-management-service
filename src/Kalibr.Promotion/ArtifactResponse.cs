namespace Kalibr.Promotion;

/// <summary>
/// The evidence a candidate gave for a competency.
/// </summary>
/// <param name="CompetencyId">The competency.</param>
/// <param name="CompetencyName">The competency name as the bound version froze it.</param>
/// <param name="Links">The artifacts, in the order the candidate gave them.</param>
internal sealed record ArtifactResponse(Guid CompetencyId, string CompetencyName, IReadOnlyList<ArtifactLink> Links);
