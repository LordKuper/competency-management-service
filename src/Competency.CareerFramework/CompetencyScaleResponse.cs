namespace Competency.CareerFramework;

/// <summary>
/// A scale as the API returns it, with its points in ascending order of score and whether it is the default scale. The version equals the <c>ETag</c> a change returns
/// and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record CompetencyScaleResponse(
    Guid Id,
    string Name,
    IReadOnlyList<ScalePoint> Points,
    bool IsDefault,
    CatalogStatus Status,
    int Version)
{
    public static CompetencyScaleResponse From(CompetencyScale scale) =>
        new(scale.Id, scale.Name, scale.Points, scale.IsDefault, scale.Status, scale.Version);
}
