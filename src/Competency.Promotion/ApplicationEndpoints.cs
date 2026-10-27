using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// The API of applications. An employee files an application for themselves while the wave takes them, changes it until that stage ends, withdraws
/// it until a decision is recorded, and lists their own. Everyone who may see an application reads it, shaped by their role. A change takes the
/// promotion locks and the rows of the wave and the application, and an application its reader may not see is reported as not found.
/// </summary>
internal static class ApplicationEndpoints
{
    private const string Route = "/api/v1/promotion/applications";
    private const string Tag = "Promotion";
    private const int SubmissionStage = 1;
    private const string NotSubmissionStageMessage = "Заявки принимаются на этапе «Подача заявок»";
    private const string AlreadyAppliedMessage = "В этой волне у вас уже есть заявка";
    private const string EditClosedMessage = "Заявку можно изменить до конца этапа «Подача заявок»";
    private const string WithdrawClosedMessage = "Заявку нельзя отозвать: по ней уже принято решение или она отклонена";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost(string.Empty, CreateAsync).WithName("CreatePromotionApplication").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/mine", ListMineAsync).WithName("ListMyPromotionApplications");
        group.MapGet("/{id:guid}", GetAsync).WithName("GetPromotionApplication").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdatePromotionApplication").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/withdraw", WithdrawAsync).WithName("WithdrawPromotionApplication").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Files an application of the signed-in employee. They work and have an account that can act, the wave takes applications, they have no other
    /// unfinished application in it, and the target, the scores and the raters are valid.
    /// </summary>
    private static async Task<Results<Created<ApplicationResponse>, NotFound, ValidationProblem, ProblemHttpResult>> CreateAsync(
        NewApplicationRequest request,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ApplicationRules rules,
        ApplicationReader reader,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var shapeErrors = request.Validate();
        if (shapeErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(shapeErrors);
        }

        await using var transaction = await locks.BeginSharedAsync(cancellationToken);
        if (await locks.FindWaveForUpdateAsync(request.WaveId, cancellationToken) is not { } wave)
        {
            return TypedResults.NotFound();
        }

        if (await access.ActingCandidateAsync(cancellationToken) is not { } candidate)
        {
            return Rejection.Forbidden();
        }

        if (calendar.StageOf(wave) != SubmissionStage)
        {
            return Rejection.Conflict(NotSubmissionStageMessage);
        }

        if (await context.Set<PromotionApplication>()
            .AnyAsync(other => other.WaveId == wave.Id && other.EmployeeId == candidate.Id && PromotionApplication.UnfinishedStatuses.Contains(other.Status), cancellationToken))
        {
            return Rejection.Conflict(AlreadyAppliedMessage);
        }

        var (target, errors) = await rules.CheckAsync(wave, request, candidate, cancellationToken);
        if (target is null || errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var application = new PromotionApplication { WaveId = wave.Id, EmployeeId = candidate.Id };
        context.Add(application);
        await ApplicationContent.ApplyAsync(context, application, target, request, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Created($"{Route}/{application.Id}", await reader.DescribeAsync(application, wave, ApplicationRole.Candidate, cancellationToken));
    }

    private static async Task<Ok<IReadOnlyList<ApplicationSummaryResponse>>> ListMineAsync(
        AppDbContext context,
        ApplicationAccess access,
        ApplicationReader reader,
        CancellationToken cancellationToken)
    {
        if (await access.EmployeeIdAsync(cancellationToken) is not { } employeeId)
        {
            return TypedResults.Ok<IReadOnlyList<ApplicationSummaryResponse>>([]);
        }

        var applications = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.EmployeeId == employeeId)
            .OrderByDescending(application => application.CreatedAt)
            .ThenBy(application => application.Id)
            .ToListAsync(cancellationToken);
        var waveIds = applications.Select(application => application.WaveId).Distinct().ToArray();
        var waves = await context.Set<PromotionWave>().AsNoTracking().Where(wave => waveIds.Contains(wave.Id)).ToDictionaryAsync(wave => wave.Id, cancellationToken);
        return TypedResults.Ok(await reader.SummariesAsync(applications, waves, new ApplicationViewer(employeeId, ApplicationRole.None, new Dictionary<Guid, ApplicationRole>()), cancellationToken));
    }

    private static async Task<Results<Ok<ApplicationResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        ApplicationAccess access,
        ApplicationReader reader,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var application = await context.Set<PromotionApplication>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (application is null)
        {
            return TypedResults.NotFound();
        }

        var wave = await context.Set<PromotionWave>().AsNoTracking().SingleAsync(candidate => candidate.Id == application.WaveId, cancellationToken);
        var roles = (await access.ViewerAsync(wave, [application.OrgUnitId], cancellationToken)).RolesOf(application);
        if (roles == ApplicationRole.None)
        {
            return TypedResults.NotFound();
        }

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Replaces the content of an application while the wave takes applications; the target may change within the wave.
    /// </summary>
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        ApplicationRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ApplicationRules rules,
        ApplicationReader reader,
        HttpResponse response,
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

        var roles = (await access.ViewerAsync(wave, [application.OrgUnitId], cancellationToken)).RolesOf(application);
        if (roles == ApplicationRole.None)
        {
            return TypedResults.NotFound();
        }

        if (roles != ApplicationRole.Candidate)
        {
            return Rejection.Forbidden();
        }

        if (calendar.StageOf(wave) != SubmissionStage || application.Status != ApplicationStatus.Submitted)
        {
            return Rejection.Conflict(EditClosedMessage);
        }

        if (await access.ActingCandidateAsync(cancellationToken) is not { } candidate)
        {
            return Rejection.Forbidden();
        }

        ifMatch.ApplyTo(context, application);
        var (target, errors) = await rules.CheckAsync(wave, request, candidate, cancellationToken);
        if (target is null || errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await ApplicationContent.ApplyAsync(context, application, target, request, cancellationToken);
        context.Entry(application).Property(entry => entry.Version).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Withdraws an application that is still going and has no recorded decision.
    /// </summary>
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ProblemHttpResult>> WithdrawAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        ApplicationAccess access,
        ApplicationReader reader,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await locks.BeginSharedAsync(cancellationToken);
        if (await locks.FindApplicationForUpdateAsync(id, cancellationToken) is not var (wave, application))
        {
            return TypedResults.NotFound();
        }

        var roles = (await access.ViewerAsync(wave, [application.OrgUnitId], cancellationToken)).RolesOf(application);
        if (roles == ApplicationRole.None)
        {
            return TypedResults.NotFound();
        }

        if (roles != ApplicationRole.Candidate)
        {
            return Rejection.Forbidden();
        }

        if (!application.CanBeWithdrawn)
        {
            return Rejection.Conflict(WithdrawClosedMessage);
        }

        ifMatch.ApplyTo(context, application);
        application.Status = ApplicationStatus.Withdrawn;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }
}
