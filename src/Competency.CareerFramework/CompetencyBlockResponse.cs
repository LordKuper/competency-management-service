namespace Competency.CareerFramework;

/// <summary>
/// A shared block as the API returns it, with its scale and its competencies by name in the order of the composition. The version equals
/// the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>. <c>CanEdit</c> and <c>CanDelete</c> say whether the signed-in user
/// may change and delete the block now, so a client offers only what the server allows.
/// </summary>
internal sealed record CompetencyBlockResponse(
    Guid Id,
    string Name,
    Guid ScaleId,
    string ScaleName,
    IReadOnlyList<CareerNamedResponse> Competencies,
    CatalogStatus Status,
    int Version,
    bool CanEdit,
    bool CanDelete)
{
    public static CompetencyBlockResponse From(CompetencyBlock block, IReadOnlyDictionary<Guid, string> names, CatalogViewer viewer) => new(
        block.Id,
        block.Name,
        block.ScaleId,
        names.GetValueOrDefault(block.ScaleId, string.Empty),
        [.. block.CompetencyIds.Select(id => new CareerNamedResponse(id, names.GetValueOrDefault(id, string.Empty)))],
        block.Status,
        block.Version,
        viewer.CanEdit(block),
        viewer.CanDelete(block));
}
