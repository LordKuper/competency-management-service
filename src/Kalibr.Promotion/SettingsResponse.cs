namespace Competency.Promotion;

/// <summary>
/// The promotion settings as the API returns them.
/// </summary>
internal sealed record SettingsResponse(int MinIntervalMonths, IReadOnlyList<int> StageDurationsDays)
{
    public static SettingsResponse From(PromotionSettings settings) => new(settings.MinIntervalMonths, settings.StageDurationsDays);
}
