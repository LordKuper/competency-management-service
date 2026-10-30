namespace Kalibr.Promotion;

/// <summary>
/// The answers of a rater about a candidate, one for every competency of the version. The set is replaced.
/// </summary>
internal sealed record RatingRequest
{
    private const int MaxAnswers = 500;

    public required RatingAnswerInput[] Answers { get; init; }

    /// <summary>
    /// Checks the size of the set.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate() =>
        Answers is { Length: <= MaxAnswers } && Answers.All(answer => answer is not null)
            ? []
            : new Dictionary<string, string[]> { ["answers"] = ["Оцените каждую компетенцию"] };
}
