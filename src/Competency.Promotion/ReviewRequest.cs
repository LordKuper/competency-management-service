namespace Competency.Promotion;

/// <summary>
/// What the chair records for a candidate: the score of every competency and, when they have reached one, the decision. Everything is replaced.
/// </summary>
internal sealed record ReviewRequest
{
    private const int MaxScores = 500;

    public required FinalScoreInput[] FinalScores { get; init; }

    /// <summary>
    /// The decision; null while the chair has not made one. A decision once recorded can be changed but not withdrawn.
    /// </summary>
    public PromotionDecision? Decision { get; init; }

    /// <summary>
    /// Checks the shape of the request.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (FinalScores is not { Length: <= MaxScores } || FinalScores.Any(score => score is null))
        {
            errors["finalScores"] = ["Оцените каждую компетенцию"];
        }

        if (Decision is { } decision && !Enum.IsDefined(decision))
        {
            errors["decision"] = ["Выберите решение из предложенных"];
        }

        return errors;
    }
}
