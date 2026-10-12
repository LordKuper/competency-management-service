namespace Competency.CareerFramework;

/// <summary>
/// The state of a competency after one saved change, kept so its history can be read. Rows are only ever added, and deleted
/// only with their competency; the journal already records the change itself, so adding a row is not journaled again.
/// </summary>
internal sealed class CompetencyVersion
{
    public Guid CompetencyId { get; init; }

    /// <summary>
    /// The version number, from 1 for the competency as created; one more than the competency's concurrency version at that point.
    /// </summary>
    public int Number { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public Guid CategoryId { get; init; }

    public Guid? DirectionId { get; init; }

    public CompetencyType Type { get; init; }

    public Guid ScaleId { get; init; }

    public CatalogStatus Status { get; init; }

    public Guid? ChangedById { get; init; }

    public DateTimeOffset ChangedAt { get; init; }

    public static CompetencyVersion Of(CareerCompetency competency, Guid? changedById, DateTimeOffset changedAt) => new()
    {
        CompetencyId = competency.Id,
        Number = NumberOf(competency),
        Name = competency.Name,
        Description = competency.Description,
        CategoryId = competency.CategoryId,
        DirectionId = competency.DirectionId,
        Type = competency.Type,
        ScaleId = competency.ScaleId,
        Status = competency.Status,
        ChangedById = changedById,
        ChangedAt = changedAt,
    };

    /// <summary>
    /// The number of the competency's latest version, as it stands after its last save.
    /// </summary>
    /// <param name="competency">The saved competency.</param>
    /// <returns>The version number.</returns>
    public static int NumberOf(CareerCompetency competency) => competency.Version + 1;
}
