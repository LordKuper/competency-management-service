using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Checks what a candidate puts in an application: a target the wave offers, scores on the scale of its version and raters who can rate. A draft
/// may leave scores and raters out; the check made when the application is filed wants a score for every competency and enough raters.
/// A caller holds the promotion locks first. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class ApplicationRules(AppDbContext context, ICareerModel career, IEmployeeDirectory employees, IUserDirectory users)
{
    private const int MinRaters = 5;
    private const int MinRatersOutsideUnit = 2;
    private const string MatrixField = "matrixId";
    private const string LevelField = "levelId";
    private const string ScoresField = "selfScores";
    private const string RatersField = "raters";

    /// <summary>
    /// Checks the content of an application against the wave.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="request">The content.</param>
    /// <param name="candidate">The candidate.</param>
    /// <param name="isComplete">Whether the content must be complete, as it must when the application is filed.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The target when the request names one the wave offers, and the problems found by field name; the content is valid when there are none.</returns>
    public async Task<(ResolvedTarget? Target, Dictionary<string, string[]> Errors)> CheckAsync(
        PromotionWave wave,
        ApplicationRequest request,
        EmployeeStatus candidate,
        bool isComplete,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var matrix = await context.Set<PromotionWaveMatrix>()
            .AsNoTracking()
            .FirstOrDefaultAsync(bound => bound.WaveId == wave.Id && bound.MatrixId == request.MatrixId, cancellationToken);
        if (matrix is null || await career.VersionAsync(matrix.VersionId, cancellationToken) is not { } version)
        {
            errors[MatrixField] = ["Выберите подразделение и трек из предложенных"];
            return (null, errors);
        }

        var level = version.Levels.FirstOrDefault(candidateLevel => candidateLevel.Id == request.LevelId);
        if (level is null)
        {
            errors[LevelField] = ["Выберите уровень из предложенных"];
        }

        var scores = request.SelfScores.Select(score => (score.CompetencyId, (int?)score.Score)).ToList();
        await AddContentProblemsAsync(errors, version, scores, request.Raters, candidate, isComplete, cancellationToken);
        return (level is null ? null : new ResolvedTarget(matrix, version, level), errors);
    }

    /// <summary>
    /// Checks a draft that is being filed: the whole content it holds must be complete.
    /// </summary>
    /// <param name="application">The draft.</param>
    /// <param name="candidate">The candidate.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The problems found by field name; the application may be filed when there are none.</returns>
    public async Task<Dictionary<string, string[]>> CheckFiledAsync(PromotionApplication application, EmployeeStatus candidate, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var version = (await career.VersionAsync(application.VersionId, cancellationToken))!;
        var scores = await context.Set<PromotionSelfScore>()
            .AsNoTracking()
            .Where(score => score.ApplicationId == application.Id)
            .Select(score => new { score.CompetencyId, score.Score })
            .ToListAsync(cancellationToken);
        var raters = await context.Set<PromotionRater>()
            .AsNoTracking()
            .Where(rater => rater.ApplicationId == application.Id)
            .Select(rater => new RaterInput(rater.EmployeeId, rater.Email))
            .ToListAsync(cancellationToken);
        await AddContentProblemsAsync(errors, version, [.. scores.Select(score => (score.CompetencyId, (int?)score.Score))], raters, candidate, isComplete: true, cancellationToken);
        return errors;
    }

    /// <summary>
    /// Says why employees cannot be the raters of a candidate: they must exist, work, not be the candidate and not have a blocked account, and one
    /// without an account needs an address that is free. A complete list also has at least five raters, at least two of them from other units than the candidate's own.
    /// </summary>
    /// <param name="candidate">The candidate.</param>
    /// <param name="raters">The proposed raters.</param>
    /// <param name="isComplete">Whether the list must meet the minimums and every rater without an account must have an address.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>One line per problem, addressed to the user; empty when the raters are fine.</returns>
    public async Task<List<string>> RaterProblemsAsync(EmployeeStatus candidate, IReadOnlyCollection<RaterInput> raters, bool isComplete, CancellationToken cancellationToken)
    {
        var listed = raters.GroupBy(rater => rater.EmployeeId).Select(group => group.First()).ToList();
        var ids = listed.Select(rater => rater.EmployeeId).ToList();
        var people = await employees.FindManyAsync(ids, cancellationToken);
        var accounts = await users.FindByEmployeeIdsAsync(ids, cancellationToken);
        List<string> problems = [];
        if (ids.Contains(candidate.Id))
        {
            problems.Add("Кандидат не может оценивать себя");
        }

        foreach (var rater in listed.Where(rater => rater.EmployeeId != candidate.Id))
        {
            problems.AddRange(await RaterProblemsAsync(rater, people, accounts, isComplete, cancellationToken));
        }

        if (listed.Where(rater => !accounts.ContainsKey(rater.EmployeeId) && !string.IsNullOrWhiteSpace(rater.Email))
            .GroupBy(rater => rater.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            problems.Add("У разных оценивающих не может быть одной и той же почты");
        }

        if (isComplete && ids.Count < MinRaters)
        {
            problems.Add($"Укажите не меньше {MinRaters} оценивающих");
        }

        if (isComplete && people.Values.Count(person => person.OrgUnitId != candidate.OrgUnitId) < MinRatersOutsideUnit)
        {
            problems.Add($"Не меньше {MinRatersOutsideUnit} оценивающих должны быть из других подразделений");
        }

        return problems;
    }

    private async Task AddContentProblemsAsync(
        Dictionary<string, string[]> errors,
        CareerVersion version,
        IReadOnlyCollection<(Guid CompetencyId, int? Score)> scores,
        IReadOnlyCollection<RaterInput> raters,
        EmployeeStatus candidate,
        bool isComplete,
        CancellationToken cancellationToken)
    {
        if (ScoreRules.Problems(version, scores, allowNotObserved: false, requireAll: isComplete) is { Count: > 0 } scoreProblems)
        {
            errors[ScoresField] = [.. scoreProblems];
        }

        if (await RaterProblemsAsync(candidate, raters, isComplete, cancellationToken) is { Count: > 0 } raterProblems)
        {
            errors[RatersField] = [.. raterProblems];
        }
    }

    private async Task<List<string>> RaterProblemsAsync(
        RaterInput rater,
        IReadOnlyDictionary<Guid, EmployeeStatus> people,
        IReadOnlyDictionary<Guid, UserStatus> accounts,
        bool isComplete,
        CancellationToken cancellationToken)
    {
        if (!people.TryGetValue(rater.EmployeeId, out var person))
        {
            return ["Сотрудник не найден"];
        }

        if (!person.IsActive)
        {
            return [$"Сотрудник «{person.FullName}» не работает"];
        }

        if (accounts.TryGetValue(rater.EmployeeId, out var account))
        {
            return account.IsBlocked ? [$"У сотрудника «{person.FullName}» заблокирована учётная запись"] : [];
        }

        if (string.IsNullOrWhiteSpace(rater.Email))
        {
            return isComplete ? [$"Укажите электронную почту сотрудника «{person.FullName}»: у него нет учётной записи"] : [];
        }

        return await users.EmailProblemAsync(rater.Email, cancellationToken) is { } problem ? [$"Почта сотрудника «{person.FullName}»: {problem}"] : [];
    }
}
