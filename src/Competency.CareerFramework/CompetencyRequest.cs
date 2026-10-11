namespace Competency.CareerFramework;

/// <summary>
/// The fields of a new competency or of a change of one; every field but the scale must be sent, an absent value as null.
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

    /// <summary>
    /// The scale; absent or null for the default scale.
    /// </summary>
    public Guid? ScaleId { get; init; }

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
    /// <param name="scaleId">The scale the request names, or else the default scale.</param>
    public void ApplyTo(CareerCompetency competency, Guid scaleId)
    {
        competency.Name = TrimmedName();
        competency.Description = OptionalText(Description);
        competency.CategoryId = CategoryId;
        competency.DirectionId = DirectionId;
        competency.Type = Type;
        competency.ScaleId = scaleId;
    }
}
