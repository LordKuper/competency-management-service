namespace Competency.CareerFramework;

/// <summary>
/// The name, scale, unit and composition of a new shared block or of a change of one; the composition replaces the block's as a whole.
/// </summary>
internal sealed record CompetencyBlockRequest : CatalogNameRequest
{
    public const string CompetenciesField = "competencyIds";

    public required Guid ScaleId { get; init; }

    /// <summary>
    /// The active unit the block applies to, with the units below it; absent or null for a block that applies to all.
    /// </summary>
    public Guid? ScopeUnitId { get; init; }

    public required IReadOnlyList<Guid> CompetencyIds { get; init; }

    /// <inheritdoc />
    public override Dictionary<string, string[]> Validate()
    {
        var errors = base.Validate();
        var ids = CompetencyIds ?? [];
        if (ids.Distinct().Count() != ids.Count)
        {
            errors[CompetenciesField] = ["Компетенция входит в состав блока не больше одного раза"];
        }

        return errors;
    }

    /// <summary>
    /// The composition as stored; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <returns>The competency identifiers in the order sent.</returns>
    public List<Guid> StoredCompetencyIds() => [.. CompetencyIds];
}
