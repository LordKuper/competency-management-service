using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// The API of the circular rating. A rater lists the candidates they were asked to rate, sees the name of the candidate and the competencies, and gives
/// or changes their answers while the wave rates. Nothing else about the application, and no answer of anybody else, reaches a rater. An
/// application the caller has no part in is reported as not found, and one they see without being its rater as forbidden.
/// </summary>
internal static class RatingEndpoints
{
    private const string Tag = "Promotion";
    private const int RatingStage = 3;
    private const string RatingClosedMessage = "Оценку можно дать и изменить на этапе «Круговая оценка»";

    private static readonly ApplicationStatus[] RatedStatuses =
        [ApplicationStatus.Admitted, ApplicationStatus.UnderReview, ApplicationStatus.Decided, ApplicationStatus.NoDecision];

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/promotion")
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/ratings/mine", ListMineAsync).WithName("ListMyPromotionRatings");
        group.MapGet("/applications/{id:guid}/rating", GetAsync).WithName("GetPromotionRating")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/applications/{id:guid}/rating", PutAsync).WithName("PutPromotionRating")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Ok<IReadOnlyList<RatingTaskResponse>>> ListMineAsync(
        AppDbContext context,
        ApplicationAccess access,
        IEmployeeDirectory employees,
        PromotionCalendar calendar,
        CancellationToken cancellationToken)
    {
        if (await access.EmployeeIdAsync(cancellationToken) is not { } employeeId)
        {
            return TypedResults.Ok<IReadOnlyList<RatingTaskResponse>>([]);
        }

        var asked = await context.Set<PromotionRater>()
            .AsNoTracking()
            .Where(rater => rater.EmployeeId == employeeId)
            .Join(context.Set<PromotionApplication>().AsNoTracking(), rater => rater.ApplicationId, application => application.Id, (rater, application) => application)
            .Where(application => RatedStatuses.Contains(application.Status))
            .OrderByDescending(application => application.CreatedAt)
            .ThenBy(application => application.Id)
            .ToListAsync(cancellationToken);
        var waveIds = asked.Select(application => application.WaveId).Distinct().ToArray();
        var waves = await context.Set<PromotionWave>().AsNoTracking().Where(wave => waveIds.Contains(wave.Id)).ToDictionaryAsync(wave => wave.Id, cancellationToken);
        var rated = (await context.Set<PromotionRaterScore>()
                .AsNoTracking()
                .Where(answer => answer.EmployeeId == employeeId)
                .Select(answer => answer.ApplicationId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var people = await employees.FindManyAsync([.. asked.Select(application => application.EmployeeId).Distinct()], cancellationToken);

        return TypedResults.Ok<IReadOnlyList<RatingTaskResponse>>([.. asked.Select(application =>
        {
            var stage = calendar.StageOf(waves[application.WaveId]);
            return new RatingTaskResponse(
                application.Id,
                application.WaveId,
                people[application.EmployeeId].FullName,
                stage,
                SessionDatesResponse.From(waves[application.WaveId], stage),
                IsOpen(stage, application),
                rated.Contains(application.Id));
        })]);
    }

    private static async Task<Results<Ok<RatingResponse>, NotFound, ProblemHttpResult>> GetAsync(
        Guid id,
        AppDbContext context,
        ApplicationAccess access,
        ICareerModel career,
        IEmployeeDirectory employees,
        PromotionCalendar calendar,
        CancellationToken cancellationToken)
    {
        var application = await context.Set<PromotionApplication>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (application is null)
        {
            return TypedResults.NotFound();
        }

        var wave = await context.Set<PromotionWave>().AsNoTracking().SingleAsync(candidate => candidate.Id == application.WaveId, cancellationToken);
        if (await access.EmployeeIdAsync(cancellationToken) is not { } employeeId || !await IsRaterAsync(context, application, employeeId, cancellationToken))
        {
            if (await IsVisibleAsync(access, wave, application, cancellationToken))
            {
                return Rejection.Forbidden();
            }

            return TypedResults.NotFound();
        }

        return TypedResults.Ok(await DescribeAsync(context, career, employees, calendar, wave, application, employeeId, cancellationToken));
    }

    /// <summary>
    /// Replaces the answers of the signed-in rater about the candidate, one for every competency of the version. Answers of other raters are not touched.
    /// </summary>
    private static async Task<Results<Ok<RatingResponse>, NotFound, ValidationProblem, ProblemHttpResult>> PutAsync(
        Guid id,
        RatingRequest request,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ICareerModel career,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        var shapeErrors = request.Validate();
        if (shapeErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(shapeErrors);
        }

        await using var transaction = await locks.BeginSharedAsync(cancellationToken);
        if (await locks.FindApplicationForUpdateAsync(id, cancellationToken) is not var (wave, application))
        {
            return TypedResults.NotFound();
        }

        if (await access.EmployeeIdAsync(cancellationToken) is not { } employeeId || !await IsRaterAsync(context, application, employeeId, cancellationToken))
        {
            if (await IsVisibleAsync(access, wave, application, cancellationToken))
            {
                return Rejection.Forbidden();
            }

            return TypedResults.NotFound();
        }

        if (!IsOpen(calendar.StageOf(wave), application))
        {
            return Rejection.Conflict(RatingClosedMessage);
        }

        if (await access.ActingCandidateAsync(cancellationToken) is null)
        {
            return Rejection.Forbidden();
        }

        var version = (await career.VersionAsync(application.VersionId, cancellationToken))!;
        var answers = request.Answers.Select(answer => (answer.CompetencyId, answer.Score)).ToList();
        if (ScoreRules.Problems(version, answers, allowNotObserved: true) is { Count: > 0 } problems)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["answers"] = [.. problems] });
        }

        await ReplaceAnswersAsync(context, application.Id, employeeId, request.Answers, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return TypedResults.Ok(await DescribeAsync(context, career, employees, calendar, wave, application, employeeId, cancellationToken));
    }

    private static bool IsOpen(int stage, PromotionApplication application) => stage == RatingStage && application.Status == ApplicationStatus.Admitted;

    private static async Task<bool> IsRaterAsync(AppDbContext context, PromotionApplication application, Guid employeeId, CancellationToken cancellationToken) =>
        RatedStatuses.Contains(application.Status)
        && await context.Set<PromotionRater>().AnyAsync(rater => rater.ApplicationId == application.Id && rater.EmployeeId == employeeId, cancellationToken);

    private static async Task<bool> IsVisibleAsync(ApplicationAccess access, PromotionWave wave, PromotionApplication application, CancellationToken cancellationToken) =>
        (await access.ViewerAsync(wave, [application.OrgUnitId], cancellationToken)).RolesOf(application) != ApplicationRole.None;

    private static async Task<RatingResponse> DescribeAsync(
        AppDbContext context,
        ICareerModel career,
        IEmployeeDirectory employees,
        PromotionCalendar calendar,
        PromotionWave wave,
        PromotionApplication application,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var version = (await career.VersionAsync(application.VersionId, cancellationToken))!;
        var own = await context.Set<PromotionRaterScore>()
            .AsNoTracking()
            .Where(answer => answer.ApplicationId == application.Id && answer.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
        var stage = calendar.StageOf(wave);
        return new RatingResponse(
            application.Id,
            (await employees.FindAsync(application.EmployeeId, cancellationToken))!.FullName,
            stage,
            SessionDatesResponse.From(wave, stage),
            IsOpen(stage, application),
            [.. version.Competencies.Select(competency => TargetFormCompetencyResponse.Of(version, competency, withRequirements: false))],
            own.Count == 0 ? null : [.. own.Select(answer => new RatingAnswerInput(answer.CompetencyId, answer.Score))]);
    }

    private static async Task ReplaceAnswersAsync(
        AppDbContext context,
        Guid applicationId,
        Guid employeeId,
        IReadOnlyCollection<RatingAnswerInput> answers,
        CancellationToken cancellationToken)
    {
        var given = answers.ToDictionary(answer => answer.CompetencyId, answer => answer.Score);
        var stored = await context.Set<PromotionRaterScore>().Where(answer => answer.ApplicationId == applicationId && answer.EmployeeId == employeeId).ToListAsync(cancellationToken);
        context.RemoveRange(stored.Where(answer => !given.ContainsKey(answer.CompetencyId)));
        foreach (var answer in stored.Where(answer => given.ContainsKey(answer.CompetencyId)))
        {
            answer.Score = given[answer.CompetencyId];
        }

        context.AddRange(given
            .Where(pair => stored.All(answer => answer.CompetencyId != pair.Key))
            .Select(pair => new PromotionRaterScore { ApplicationId = applicationId, EmployeeId = employeeId, CompetencyId = pair.Key, Score = pair.Value }));
    }
}
