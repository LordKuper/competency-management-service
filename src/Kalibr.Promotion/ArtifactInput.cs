namespace Competency.Promotion;

/// <summary>
/// The evidence a candidate offers for one competency: up to <see cref="MaxLinks"/> artifacts.
/// </summary>
/// <param name="CompetencyId">The competency of the matrix version.</param>
/// <param name="Links">The artifacts, each a description and a link to a document kept in another system.</param>
internal sealed record ArtifactInput(Guid CompetencyId, ArtifactLink[] Links)
{
    public const int MaxLinks = 10;
}
