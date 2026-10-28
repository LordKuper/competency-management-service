using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// The parts of an application filled in around the stages: the responsible HR of the unit of the target enters the dates for the formal check from
/// the moment the application is filed until that stage ends, the candidate lists the raters in the draft and the head of that unit corrects them
/// over the same period, the head or the responsible HR confirms the address of a rater without an account, the candidate adds artifacts in the draft
/// and while they are collected, sends the application to the commission when it owes artifacts, and marks the feedback as received. Each change takes the promotion locks and the rows of the wave and the application.
/// </summary>
internal static class ApplicationStageEndpoints
{
    private const string Route = "/api/v1/promotion/applications/{id:guid}";
    private const string Tag = "Promotion";
    private const string DateField = "hiredOrPromotedOn";
    private const string FutureDateMessage = "Дата не позже сегодняшнего дня";
    private const string FormalClosedMessage = "Даты вносятся с отправки заявки до конца этапа «Формальная проверка»";
    private const string ArtifactsClosedMessage = "Артефакты добавляют в черновике и на этапе «Сбор артефактов» до отправки на рассмотрение";
    private const string SendClosedMessage = "Заявку можно отправить на рассмотрение на этапе «Сбор артефактов», пока она ещё не у комиссии";
    private const string NoChairMessage = "У комиссии подразделения нет председателя, заявку пока нельзя отправить";
    private const string FeedbackClosedMessage = "Отметку можно поставить после публикации результатов, на этапе «Обратная связь»";
    private const string RatersClosedMessage = "Список оценивающих меняет кандидат в черновике, а руководитель — с отправки заявки до конца этапа «Формальная проверка»";
    private const string EmailClosedMessage = "Почту подтверждают с отправки заявки до конца этапа «Формальная проверка»";
    private const string RaterNotFoundMessage = "Этого сотрудника нет среди оценивающих";
    private const string RaterHasAccountMessage = "У сотрудника есть учётная запись, почта не нужна";
    private const string EmailRepeatedMessage = "У другого оценивающего этой заявки уже указана такая почта";

    private static readonly ApplicationStatus[] ArtifactStatuses = [ApplicationStatus.Admitted, ApplicationStatus.UnderReview, ApplicationStatus.Decided];

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
        group.MapPut("/raters/{employeeId:guid}/email", UpdateRaterEmailAsync).WithName("UpdatePromotionRaterEmail").ProducesETag();
        group.MapPut("/artifacts", UpdateArtifactsAsync).WithName("UpdatePromotionApplicationArtifacts").ProducesETag();
        group.MapPost("/send-to-review", SendToReviewAsync).WithName("SendPromotionApplicationToReview").ProducesETag();
        group.MapPost("/feedback", MarkFeedbackAsync).WithName("MarkPromotionFeedbackReceived").ProducesETag();
    }

    /// <summary>
    /// Enters the date of hiring or of the last promotion and whether the interval is ignored, for the responsible HR of the unit of the target from
    /// the moment the application is filed until the formal check stage ends. The answer carries the result the check would give now.
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

        if (!IsListOpen(calendar.StageOf(wave), application))
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
    /// Replaces the raters of an application: by the candidate in the draft, by the head of the unit of the target from the moment the application is
    /// filed until the formal check stage ends, when the list becomes final. A draft may hold fewer raters than the minimum, a filed application may not.
    /// An address the head enters or changes is confirmed by that.
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
        IUserDirectory users,
        ICurrentActor actor,
        TimeProvider time,
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

        var isCandidate = roles == ApplicationRole.Candidate;
        if (!isCandidate && !roles.HasFlag(ApplicationRole.Head))
        {
            return Rejection.Forbidden();
        }

        var stage = calendar.StageOf(wave);
        if (isCandidate ? stage != PromotionStage.Submission || application.Status != ApplicationStatus.Draft : !IsListOpen(stage, application))
        {
            return Rejection.Conflict(RatersClosedMessage);
        }

        ifMatch.ApplyTo(context, application);
        var candidate = (await employees.FindAsync(application.EmployeeId, cancellationToken))!;
        if (await rules.RaterProblemsAsync(candidate, request.Raters, isComplete: !isCandidate, application.Id, cancellationToken) is { Count: > 0 } problems)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["raters"] = [.. problems] });
        }

        var edit = new RaterEdit(request.Raters, isCandidate ? RaterSource.Candidate : RaterSource.Head, isCandidate ? null : actor.UserId, time.GetUtcNow());
        await ApplicationContent.ReplaceRatersAsync(context, users, application.Id, edit, cancellationToken);
        context.Entry(application).Property(entry => entry.Version).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Enters or confirms the address of a rater who has no account, for the head of the unit of the target and for the responsible HR, from the moment
    /// the application is filed until the formal check stage ends. The invitation goes only to a confirmed address.
    /// </summary>
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateRaterEmailAsync(
        Guid id,
        Guid employeeId,
        RaterEmailRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ApplicationRules rules,
        ApplicationReader reader,
        IEmployeeDirectory employees,
        IUserDirectory users,
        ICurrentActor actor,
        TimeProvider time,
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

        if (!roles.HasFlag(ApplicationRole.Head) && !roles.HasFlag(ApplicationRole.ResponsibleHr))
        {
            return Rejection.Forbidden();
        }

        if (!IsListOpen(calendar.StageOf(wave), application))
        {
            return Rejection.Conflict(EmailClosedMessage);
        }

        var raters = await context.Set<PromotionRater>().Where(rater => rater.ApplicationId == id).ToListAsync(cancellationToken);
        if (raters.FirstOrDefault(item => item.EmployeeId == employeeId) is not { } rater)
        {
            return Rejection.Invalid("employeeId", RaterNotFoundMessage);
        }

        var email = request.Email?.Trim() ?? string.Empty;
        if ((await users.FindByEmployeeIdsAsync([employeeId], cancellationToken)).ContainsKey(employeeId))
        {
            return Rejection.Conflict(RaterHasAccountMessage);
        }

        if (await users.EmailProblemAsync(email, cancellationToken) is { } problem)
        {
            return Rejection.Invalid("email", problem);
        }

        if (raters.Any(other => other.EmployeeId != employeeId && string.Equals(other.Email, email, StringComparison.OrdinalIgnoreCase)))
        {
            return Rejection.Invalid("email", EmailRepeatedMessage);
        }

        if (await employees.FindAsync(employeeId, cancellationToken) is { } person
            && await rules.ExclusionProblemsAsync(new Dictionary<Guid, EmployeeStatus> { [employeeId] = person }, applicationId: null, cancellationToken) is { Count: > 0 } exclusion)
        {
            return Rejection.Invalid("employeeId", exclusion[0]);
        }

        ifMatch.ApplyTo(context, application);
        rater.Email = email;
        rater.EmailConfirmedBy = actor.UserId;
        rater.EmailConfirmedAt = time.GetUtcNow();
        context.Entry(application).Property(entry => entry.Version).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Replaces the artifacts of an application in the draft and while they are collected, until the candidate sends the application to the commission;
    /// the candidate gives them for any competency of the version.
    /// </summary>
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateArtifactsAsync(
        Guid id,
        ArtifactsRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ICareerModel career,
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

        var stage = calendar.StageOf(wave);
        var isDraft = stage == PromotionStage.Submission && application.Status == ApplicationStatus.Draft;
        var isCollecting = stage == PromotionStage.Artifacts && ArtifactStatuses.Contains(application.Status) && !application.IsSentToReview;
        if (!isDraft && !isCollecting)
        {
            return Rejection.Conflict(ArtifactsClosedMessage);
        }

        ifMatch.ApplyTo(context, application);
        var version = (await career.VersionAsync(application.VersionId, cancellationToken))!;
        if (request.Artifacts.Any(artifact => version.Competencies.All(competency => competency.Id != artifact.CompetencyId)))
        {
            return Rejection.Invalid("artifacts", "Среди компетенций есть не входящая в матрицу");
        }

        await ApplicationContent.ReplaceArtifactsAsync(context, application.Id, request.Artifacts, cancellationToken);
        context.Entry(application).Property(entry => entry.Version).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Sends the application of a candidate who owes artifacts to the commission while the artifacts are collected, with whatever artifacts are given.
    /// The answer names the competencies still without an artifact, so the candidate was shown them before sending.
    /// </summary>
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ProblemHttpResult>> SendToReviewAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        StageActions actions,
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

        if (calendar.StageOf(wave) != PromotionStage.Artifacts || application.Status != ApplicationStatus.Admitted || application.MeetsAfterRating != false)
        {
            return Rejection.Conflict(SendClosedMessage);
        }

        ifMatch.ApplyTo(context, application);
        if (!await actions.SendToReviewAsync(application, cancellationToken))
        {
            return Rejection.Conflict(NoChairMessage);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Marks that the candidate received the feedback, which they may do from the publication of the results until the wave ends. Marking again changes nothing.
    /// </summary>
    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ProblemHttpResult>> MarkFeedbackAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ApplicationReader reader,
        TimeProvider time,
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

        if (calendar.StageOf(wave) != PromotionStage.Feedback || application.Status is not (ApplicationStatus.Decided or ApplicationStatus.NoDecision))
        {
            return Rejection.Conflict(FeedbackClosedMessage);
        }

        ifMatch.ApplyTo(context, application);
        application.FeedbackReceivedAt ??= time.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reader.DescribeAsync(application, wave, roles, cancellationToken));
    }

    private static bool IsListOpen(int stage, PromotionApplication application) =>
        stage is PromotionStage.Submission or PromotionStage.FormalCheck && application.Status == ApplicationStatus.Submitted;
}
