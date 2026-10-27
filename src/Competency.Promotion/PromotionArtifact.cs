using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// What a candidate offers as evidence of a competency: a description and links to documents kept elsewhere.
/// </summary>
[Audited]
internal sealed class PromotionArtifact
{
    [Audited]
    public Guid ApplicationId { get; init; }

    [Audited]
    public Guid CompetencyId { get; init; }

    [Audited]
    public string Text { get; set; } = string.Empty;

    [Audited]
    public string[] Links { get; set; } = [];
}
