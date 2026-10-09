namespace Competency.CareerFramework;

/// <summary>
/// A new competency: its fields and the unit that keeps it, which never changes afterwards.
/// </summary>
internal sealed record NewCompetencyRequest : CompetencyRequest
{
    /// <summary>
    /// The unit that keeps the competency; null for a shared competency.
    /// </summary>
    public required Guid? OwnerOrgUnitId { get; init; }
}
