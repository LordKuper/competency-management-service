namespace Competency.CareerFramework;

/// <summary>
/// The fields of a competency that may change; every field must be sent, an absent value as null.
/// </summary>
internal record CompetencyRequest : CatalogNameRequest
{
    public required string? Description { get; init; }

    public required Guid CategoryId { get; init; }

    /// <summary>
    /// The direction whose matrices may use the competency; null for a general competency.
    /// </summary>
    public required Guid? DirectionId { get; init; }

    public required CompetencyType Type { get; init; }

    public required Guid ScaleId { get; init; }

    /// <inheritdoc />
    public override Dictionary<string, string[]> Validate()
    {
        var errors = base.Validate();
        CheckText(errors, "description", Description, CareerCompetency.DescriptionMaxLength, missingMessage: null);
        return errors;
    }

    /// <summary>
    /// Copies the fields onto the competency; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <param name="competency">The competency to change.</param>
    public void ApplyTo(CareerCompetency competency)
    {
        competency.Name = TrimmedName();
        competency.Description = OptionalText(Description);
        competency.CategoryId = CategoryId;
        competency.DirectionId = DirectionId;
        competency.Type = Type;
        competency.ScaleId = ScaleId;
    }
}
