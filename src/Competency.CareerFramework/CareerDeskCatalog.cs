namespace Competency.CareerFramework;

/// <summary>
/// The catalogue entries the desk reads: the drafts the user can read, levels aside, and the directions and competencies the published matrices are checked against.
/// </summary>
/// <param name="Drafts">The drafts the user can read, levels aside.</param>
/// <param name="Directions">Every direction, by identifier.</param>
/// <param name="Competencies">The draft competencies and those the published versions name, by identifier.</param>
internal sealed record CareerDeskCatalog(
    IReadOnlyList<CatalogEntry> Drafts,
    IReadOnlyDictionary<Guid, CareerDirection> Directions,
    IReadOnlyDictionary<Guid, CareerCompetency> Competencies)
{
    /// <summary>
    /// Nothing read, for a user whose roles call for no catalogue entries.
    /// </summary>
    public static CareerDeskCatalog Empty { get; } = new([], new Dictionary<Guid, CareerDirection>(), new Dictionary<Guid, CareerCompetency>());
}
