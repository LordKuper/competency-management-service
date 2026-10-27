using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// The content of an application: the target, the candidate's education, their score for every competency of the version and the raters they propose.
/// Everything is replaced.
/// </summary>
internal record ApplicationRequest
{
    public const int MaxRaters = 50;

    private const int MaxSelfScores = 500;

    /// <summary>
    /// The matrix of the unit the target level belongs to, as the open wave offers it.
    /// </summary>
    public required Guid MatrixId { get; init; }

    public required Guid LevelId { get; init; }

    public required EducationLevel Education { get; init; }

    public required SelfScoreInput[] SelfScores { get; init; }

    /// <summary>
    /// The employees asked to rate the candidate.
    /// </summary>
    public required Guid[] RaterEmployeeIds { get; init; }

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

        if (RaterEmployeeIds is not { Length: <= MaxRaters })
        {
            errors["raterEmployeeIds"] = [$"Укажите не больше {MaxRaters} оценивающих"];
        }

        return errors;
    }
}
