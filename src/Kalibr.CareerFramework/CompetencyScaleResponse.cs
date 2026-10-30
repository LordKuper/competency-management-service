namespace Competency.CareerFramework;

/// <summary>
/// A scale as the API returns it, with its points in ascending order of score and whether it is the default scale. The version equals the <c>ETag</c> a change returns
/// and is sent back in <c>If-Match</c>. <c>CanEdit</c> and <c>CanDelete</c> say whether the signed-in user may change and delete the entry now, so a client offers only what the server allows.
/// </summary>
internal sealed record CompetencyScaleResponse(
    Guid Id,
    string Name,
    IReadOnlyList<ScalePoint> Points,
    bool IsDefault,
    CatalogStatus Status,
    int Version,
    bool CanEdit,
    bool CanDelete)
{
    public static CompetencyScaleResponse From(CompetencyScale scale, CatalogViewer viewer) =>
        new(scale.Id, scale.Name, scale.Points, scale.IsDefault, scale.Status, scale.Version, viewer.CanEdit(scale), viewer.CanDelete(scale));
}
