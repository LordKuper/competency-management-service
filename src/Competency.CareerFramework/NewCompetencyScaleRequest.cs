namespace Competency.CareerFramework;

/// <summary>
/// A new scale: its name, points and the unit that keeps it, which never changes afterwards.
/// </summary>
internal sealed record NewCompetencyScaleRequest : CompetencyScaleRequest
{
    /// <summary>
    /// The unit that keeps the scale; null for a shared scale.
    /// </summary>
    public required Guid? OwnerOrgUnitId { get; init; }
}
