using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// The content of an application: the target, the candidate's education, their scores for the competencies of the version and the raters they propose.
/// A draft may be saved with some scores and raters missing. Everything is replaced.
/// </summary>
internal record ApplicationRequest
{
    public const int MaxRaters = 50;

    private const int MaxSelfScores = 500;
    private const string RatersField = "raters";

    /// <summary>
    /// The matrix of the unit the target level belongs to, as the open wave offers it.
    /// </summary>
    public required Guid MatrixId { get; init; }

    public required Guid LevelId { get; init; }

    public required EducationLevel Education { get; init; }

    public required SelfScoreInput[] SelfScores { get; init; }

    /// <summary>
    /// The employees asked to rate the candidate, each with the address to invite them at when they have no account.
    /// </summary>
    public required RaterInput[] Raters { get; init; }

    /// <summary>
    /// Checks the shape of the request.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (!Enum.IsDefined(Education))
        {
            errors["education"] = ["Выберите уровень образования"];
        }

        if (SelfScores is not { Length: <= MaxSelfScores })
        {
            errors["selfScores"] = ["Оцените каждую компетенцию"];
        }

        if (Raters is not { Length: <= MaxRaters } || Raters.Any(rater => rater is null))
        {
            errors[RatersField] = [$"Укажите не больше {MaxRaters} оценивающих"];
        }

        return errors;
    }
}
