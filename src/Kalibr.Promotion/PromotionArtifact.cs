using Kalibr.Platform;

namespace Kalibr.Promotion;

/// <summary>
/// What a candidate offers as evidence of a competency: up to <see cref="ArtifactInput.MaxLinks"/> artifacts, each a description and a link.
/// </summary>
[Audited]
internal sealed class PromotionArtifact
{
    [Audited]
    public Guid ApplicationId { get; init; }

    [Audited]
    public Guid CompetencyId { get; init; }

    [Audited]
    public ArtifactLink[] Links { get; set; } = [];
}
