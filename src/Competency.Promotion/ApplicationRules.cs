using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Checks what a candidate puts in an application: a target the wave offers, a score for every competency of its version and raters who
/// can rate. A caller holds the promotion locks first. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class ApplicationRules(AppDbContext context, ICareerModel career, IEmployeeDirectory employees, ActingEmployees acting)
{
    private const int MinRaters = 5;
    private const int MinRatersOutsideUnit = 2;
    private const string MatrixField = "matrixId";
    private const string LevelField = "levelId";
    private const string ScoresField = "selfScores";
    private const string RatersField = "raterEmployeeIds";

    /// <summary>
    /// Checks the content of an application against the wave.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="request">The content.</param>
    /// <param name="candidate">The candidate.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The target when the request names one the wave offers, and the problems found by field name; the content is valid when there are none.</returns>
    public async Task<(ResolvedTarget? Target, Dictionary<string, string[]> Errors)> CheckAsync(
        PromotionWave wave,
        ApplicationRequest request,
        EmployeeStatus candidate,
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

        if (ScoreRules.Problems(version, [.. request.SelfScores.Select(score => (score.CompetencyId, (int?)score.Score))], allowNotObserved: false) is { Count: > 0 } scoreProblems)
        {
            errors[ScoresField] = [.. scoreProblems];
        }

        if (await RaterProblemsAsync(candidate, request.RaterEmployeeIds, cancellationToken) is { Count: > 0 } raterProblems)
        {
            errors[RatersField] = [.. raterProblems];
        }

        return (level is null ? null : new ResolvedTarget(matrix, version, level), errors);
    }

    /// <summary>
    /// Says why employees cannot be the raters of a candidate: they must work and have an account that can act, not be the candidate, be at least
    /// five, and at least two of them must belong to other units than the candidate's own.
    /// </summary>
    /// <param name="candidate">The candidate.</param>
    /// <param name="raterIds">The proposed raters.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>One line per problem, addressed to the user; empty when the raters are fine.</returns>
    public async Task<List<string>> RaterProblemsAsync(EmployeeStatus candidate, IReadOnlyCollection<Guid> raterIds, CancellationToken cancellationToken)
    {
        var ids = raterIds.Distinct().ToList();
        var problems = await acting.ProblemsAsync([.. ids.Where(id => id != candidate.Id)], cancellationToken);
        if (ids.Contains(candidate.Id))
        {
            problems.Add("Кандидат не может оценивать себя");
        }

        if (ids.Count < MinRaters)
        {
            problems.Add($"Укажите не меньше {MinRaters} оценивающих");
        }

        var people = await employees.FindManyAsync(ids, cancellationToken);
        if (people.Values.Count(person => person.OrgUnitId != candidate.OrgUnitId) < MinRatersOutsideUnit)
        {
            problems.Add($"Не меньше {MinRatersOutsideUnit} оценивающих должны быть из других подразделений");
        }

        return problems;
    }
}
