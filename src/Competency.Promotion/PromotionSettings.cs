using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// The promotion settings the global administrator keeps: the one row of its table, created by the migration.
/// </summary>
[Audited]
internal sealed class PromotionSettings : IVersioned
{
    public const int SingletonId = 1;
    public const int DefaultMinIntervalMonths = 12;
    public const int MaxIntervalMonths = 120;
    public const int MaxStageDays = 365;

    public static readonly IReadOnlyList<int> DefaultStageDurationsDays = [21, 7, 14, 7, 14, 3, 14];

    public int Id { get; init; } = SingletonId;

    /// <summary>
    /// The months that must pass between two promotions of an employee.
    /// </summary>
    [Audited]
    public int MinIntervalMonths { get; set; }

    /// <summary>
    /// The default length in days of each of the stages of a wave, in the order of the stages.
    /// </summary>
    [Audited]
    public int[] StageDurationsDays { get; set; } = [];

    /// <inheritdoc />
    public int Version { get; private set; }
}
