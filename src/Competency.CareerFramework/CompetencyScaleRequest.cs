namespace Competency.CareerFramework;

/// <summary>
/// The name and points of a scale; the points replace the scale's points as a whole.
/// </summary>
internal record CompetencyScaleRequest : CatalogNameRequest
{
    private const string PointsField = "points";

    public required IReadOnlyList<ScalePoint> Points { get; init; }

    /// <inheritdoc />
    public override Dictionary<string, string[]> Validate()
    {
        var errors = base.Validate();
        var points = Points ?? [];
        if (points.Count < CompetencyScale.MinPoints)
        {
            errors[PointsField] = [$"Укажите не меньше {CompetencyScale.MinPoints} баллов"];
        }
        else if (points.Any(point => point is null || point.Score is < 0 or > CompetencyScale.MaxScore))
        {
            errors[PointsField] = [$"Балл — целое число от 0 до {CompetencyScale.MaxScore}"];
        }
        else if (points.DistinctBy(point => point.Score).Count() != points.Count)
        {
            errors[PointsField] = ["Баллы не должны повторяться"];
        }
        else if (points.Any(point => string.IsNullOrWhiteSpace(point.Label)))
        {
            errors[PointsField] = ["Укажите значение каждого балла"];
        }
        else if (points.Any(point => point.Label.Trim().Length > CompetencyScale.LabelMaxLength))
        {
            errors[PointsField] = [$"Значение балла — не длиннее {CompetencyScale.LabelMaxLength} символов"];
        }

        return errors;
    }

    /// <summary>
    /// The points as stored: values trimmed, in ascending order of score; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <returns>The points.</returns>
    public List<ScalePoint> StoredPoints() =>
        [.. Points.Select(point => new ScalePoint(point.Score, point.Label.Trim())).OrderBy(point => point.Score)];
}
