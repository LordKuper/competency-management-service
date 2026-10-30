namespace Kalibr.CareerFramework;

/// <summary>
/// The name, unit and composition of a new shared block or of a change of one; the composition replaces the block's as a whole. A block
/// has no scale.
/// </summary>
internal sealed record CompetencyBlockRequest : CatalogNameRequest
{
    public const string CompetenciesField = "competencyIds";

    /// <summary>
    /// The active unit the block applies to, with the units below it; absent or null for a block that applies to all.
    /// </summary>
    public Guid? ScopeUnitId { get; init; }

    public required IReadOnlyList<Guid> CompetencyIds { get; init; }

    /// <inheritdoc />
    public override Dictionary<string, string[]> Validate()
    {
        var errors = base.Validate();
        if (CompetencyIds is null)
        {
            errors[CompetenciesField] = ["Укажите состав блока"];
        }
        else if (CompetencyIds.Distinct().Count() != CompetencyIds.Count)
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
