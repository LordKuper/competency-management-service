namespace Competency.CareerFramework;

/// <summary>
/// The fields of a career level that may change; every field must be sent, an absent value as null.
/// </summary>
internal record CareerLevelRequest : CatalogNameRequest
{
    public required string Code { get; init; }

    public required bool IsManagerial { get; init; }

    public required DateOnly? ValidFrom { get; init; }

    public required DateOnly? ValidTo { get; init; }

    public required string? RoleDescription { get; init; }

    public required Guid? PreviousLevelId { get; init; }

    public required IReadOnlyList<Guid> NextLevelIds { get; init; }

    /// <inheritdoc />
    public override Dictionary<string, string[]> Validate()
    {
        var errors = base.Validate();
        CheckText(errors, "code", Code, CareerLevel.CodeMaxLength, "Укажите код уровня");
        CheckText(errors, "roleDescription", RoleDescription, CareerLevel.RoleDescriptionMaxLength, missingMessage: null);
        if (ValidFrom > ValidTo)
        {
            errors["validTo"] = ["Дата окончания раньше даты начала"];
        }

        return errors;
    }

    /// <summary>
    /// The next levels as stored: each named once; an absent list means none.
    /// </summary>
    /// <returns>The distinct identifiers in request order.</returns>
    public List<Guid> DistinctNextLevelIds() => (NextLevelIds ?? []).Distinct().ToList();

    /// <summary>
    /// Copies the fields onto the level; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <param name="level">The level to change.</param>
    public void ApplyTo(CareerLevel level)
    {
        level.Name = TrimmedName();
        level.Code = Code.Trim();
        level.IsManagerial = IsManagerial;
        level.ValidFrom = ValidFrom;
        level.ValidTo = ValidTo;
        level.RoleDescription = OptionalText(RoleDescription);
        level.PreviousLevelId = PreviousLevelId;
        level.NextLevelIds = DistinctNextLevelIds();
    }
}
