using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Competency.Promotion;

/// <summary>
/// The parts of an application that other roles fill in at later stages: the responsible HR of the unit of the target enters the dates for the formal
/// check, and the raters are listed by the candidate while the wave takes applications and corrected by the head of that unit before the rating.
/// Each change takes the promotion locks and the rows of the wave and the application.
/// </summary>
internal static class ApplicationStageEndpoints
{
    private const string Route = "/api/v1/promotion/applications/{id:guid}";
    private const string Tag = "Promotion";
    private const int SubmissionStage = 1;
    private const int FormalCheckStage = 2;
    private const int RatingStage = 3;
    private const string DateField = "hiredOrPromotedOn";
    private const string FutureDateMessage = "Дата не позже сегодняшнего дня";
    private const string FormalClosedMessage = "Даты вносятся на этапе «Формальная проверка»";
    private const string RatersClosedMessage = "Список оценивающих меняют кандидат на этапе «Подача заявок» и руководитель на этапе «Круговая оценка»";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/formal", UpdateFormalAsync).WithName("UpdatePromotionApplicationFormal").ProducesETag();
        group.MapPut("/raters", UpdateRatersAsync).WithName("UpdatePromotionApplicationRaters").ProducesETag();
    }

    /// <summary>
    /// Enters the date of hiring or of the last promotion and whether the interval is ignored, for the responsible HR of the unit of the target
    /// while the formal check runs. The answer warns when the interval has not passed.
    /// </summary>
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateFormalAsync(
        Guid id,
        FormalRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ApplicationReader reader,
        ICurrentActor actor,
        TimeProvider time,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if (request.HiredOrPromotedOn > calendar.Today)
        {
            return Rejection.Invalid(DateField, FutureDateMessage);
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

        if (!roles.HasFlag(ApplicationRole.ResponsibleHr))
        {
            return Rejection.Forbidden();
        }

        if (calendar.StageOf(wave) != FormalCheckStage || application.Status != ApplicationStatus.Submitted)
        {
            return Rejection.Conflict(FormalClosedMessage);
        }

        ifMatch.ApplyTo(context, application);
        application.HiredOrPromotedOn = request.HiredOrPromotedOn;
        if (request.IgnoreInterval != application.IgnoreInterval)
        {
            application.IgnoreInterval = request.IgnoreInterval;
            application.IgnoreIntervalBy = request.IgnoreInterval ? actor.UserId : null;
            application.IgnoreIntervalAt = request.IgnoreInterval ? time.GetUtcNow() : null;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Replaces the raters of an application: by the candidate while the wave takes applications, by the head of the unit of the target while the
    /// admitted candidate is rated. The list must meet the same rules each time.
    /// </summary>
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateRatersAsync(
        Guid id,
        RatersRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ApplicationRules rules,
        ApplicationReader reader,
        IEmployeeDirectory employees,
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

        var stage = calendar.StageOf(wave);
        var source = roles == ApplicationRole.Candidate ? RaterSource.Candidate : RaterSource.Head;
        if (roles != ApplicationRole.Candidate && !roles.HasFlag(ApplicationRole.Head))
        {
            return Rejection.Forbidden();
        }

        var isOpen = source == RaterSource.Candidate
            ? stage == SubmissionStage && application.Status == ApplicationStatus.Submitted
            : stage == RatingStage && application.Status == ApplicationStatus.Admitted;
        if (!isOpen)
        {
            return Rejection.Conflict(RatersClosedMessage);
        }

        ifMatch.ApplyTo(context, application);
        var candidate = (await employees.FindAsync(application.EmployeeId, cancellationToken))!;
        if (await rules.RaterProblemsAsync(candidate, request.RaterEmployeeIds, cancellationToken) is { Count: > 0 } problems)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["raterEmployeeIds"] = [.. problems] });
        }

        await ApplicationContent.ReplaceRatersAsync(context, application.Id, request.RaterEmployeeIds, source, cancellationToken);
        context.Entry(application).Property(entry => entry.Version).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }
}
