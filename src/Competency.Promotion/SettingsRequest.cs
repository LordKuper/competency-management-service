namespace Competency.Promotion;

/// <summary>
/// The new promotion settings; both values are replaced.
/// </summary>
internal sealed record SettingsRequest
{
    public required int MinIntervalMonths { get; init; }

    public required int[] StageDurationsDays { get; init; }

    /// <summary>
    /// Checks the values.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (MinIntervalMonths is < 0 or > PromotionSettings.MaxIntervalMonths)
        {
            errors["minIntervalMonths"] = [$"Укажите число месяцев от 0 до {PromotionSettings.MaxIntervalMonths}"];
        }

        if (StageDurationsDays is not { Length: PromotionWave.StageCount })
        {
            errors["stageDurationsDays"] = [$"Укажите длительность каждого из {PromotionWave.StageCount} этапов"];
            return errors;
        }

        for (var stage = 0; stage < StageDurationsDays.Length; stage++)
        {
            if (StageDurationsDays[stage] is < 1 or > PromotionSettings.MaxStageDays)
            {
                errors[$"stageDurationsDays[{stage}]"] = [$"Укажите число дней от 1 до {PromotionSettings.MaxStageDays}"];
            }
        }

        return errors;
    }
}
