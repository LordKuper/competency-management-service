using Competency.CareerFramework;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// The API of the commission's review. The members of the commission of the unit of the target and administrators read the card of a candidate and the
/// ranking of the unit from the moment the application reaches the commission; the chair alone records the final scores and the decision, until the commission stage ends. The rating
/// and the ranking never reach the candidate. An application the caller has no part in is reported as not found, and one they see without the right to the
/// request as forbidden.
/// </summary>
internal static class ReviewEndpoints
{
    private const string Tag = "Promotion";
    private const ApplicationRole Readers = ApplicationRole.Administrator | ApplicationRole.Member;
    private const string ReviewNotStartedMessage = "Заявка поступает на рассмотрение комиссии после этапа «Круговая оценка» или, если нужны артефакты, когда они поданы";
    private const string ReviewClosedMessage = "Итоговые оценки и решение вносят с поступления заявки к комиссии до конца этапа «Рассмотрение комиссией»";
    private const string DecisionWithdrawnMessage = "Решение можно изменить, но не снять";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/promotion")
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/applications/{id:guid}/review", GetAsync).WithName("GetPromotionReview").ProducesETag()
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/applications/{id:guid}/review", PutAsync).WithName("PutPromotionReview").ProducesETag()
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/waves/{waveId:guid}/units/{unitId:guid}/ranking", RankingAsync).WithName("GetPromotionRanking");
    }

    private static async Task<Results<Ok<ReviewResponse>, NotFound, ProblemHttpResult>> GetAsync(
        Guid id,
        AppDbContext context,
        ApplicationAccess access,
        ReviewReader reviews,
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

        if ((roles & Readers) == ApplicationRole.None)
        {
            return Rejection.Forbidden();
        }

        if (!PromotionApplication.ReviewedStatuses.Contains(application.Status))
        {
            return Rejection.Conflict(ReviewNotStartedMessage);
        }

        response.SetETag(application.Version);
        return TypedResults.Ok(await reviews.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Replaces the final scores of a candidate and records or changes the decision, for the chair while the commission reviews. The scores cover
    /// every competency of the version; a decision once recorded can be changed but not withdrawn.
    /// </summary>
    private static async Task<Results<Ok<ReviewResponse>, NotFound, ValidationProblem, ProblemHttpResult>> PutAsync(
        Guid id,
        ReviewRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        ApplicationAccess access,
        ICareerModel career,
        ReviewReader reviews,
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

        if (!roles.HasFlag(ApplicationRole.Chair))
        {
            return Rejection.Forbidden();
        }

        if (calendar.StageOf(wave) > PromotionStage.Review || application.Status is not (ApplicationStatus.UnderReview or ApplicationStatus.Decided))
        {
            return Rejection.Conflict(ReviewClosedMessage);
        }

        ifMatch.ApplyTo(context, application);
        var version = (await career.VersionAsync(application.VersionId, cancellationToken))!;
        var scores = request.FinalScores.Select(score => (score.CompetencyId, (int?)score.Score)).ToList();
        if (ScoreRules.Problems(version, scores, allowNotObserved: false) is { Count: > 0 } problems)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["finalScores"] = [.. problems] });
        }

        if (request.Decision is null && application.Decision is not null)
        {
            return Rejection.Invalid("decision", DecisionWithdrawnMessage);
        }

        await ReplaceFinalScoresAsync(context, application.Id, request.FinalScores, cancellationToken);
        if (request.Decision is { } decision && decision != application.Decision)
        {
            application.Decision = decision;
            application.DecidedBy = actor.UserId;
            application.DecidedAt = time.GetUtcNow();
            application.Status = ApplicationStatus.Decided;
        }

        context.Entry(application).Property(entry => entry.Version).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(application.Version);
        return TypedResults.Ok(await reviews.DescribeAsync(application, wave, roles, cancellationToken));
    }

    /// <summary>
    /// Ranks the candidates of the commission of a unit in a wave by rating, for the members of that commission and administrators.
    /// </summary>
    private static async Task<Results<Ok<IReadOnlyList<RankingItemResponse>>, NotFound, ProblemHttpResult>> RankingAsync(
        Guid waveId,
        Guid unitId,
        AppDbContext context,
        ApplicationAccess access,
        WaveReader waves,
        ReviewReader reviews,
        CancellationToken cancellationToken)
    {
        var wave = await context.Set<PromotionWave>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == waveId, cancellationToken);
        if (wave is null)
        {
            return TypedResults.NotFound();
        }

        var viewer = await access.ViewerAsync(wave, [unitId], cancellationToken);
        if (((viewer.WaveRoles | viewer.UnitRoles.GetValueOrDefault(unitId)) & Readers) == ApplicationRole.None)
        {
            if (await waves.CanSeeAsync(wave, cancellationToken))
            {
                return Rejection.Forbidden();
            }

            return TypedResults.NotFound();
        }

        return TypedResults.Ok(await reviews.RankAsync(waveId, unitId, cancellationToken));
    }

    private static async Task ReplaceFinalScoresAsync(
        AppDbContext context,
        Guid applicationId,
        IReadOnlyCollection<FinalScoreInput> scores,
        CancellationToken cancellationToken)
    {
        var given = scores.ToDictionary(score => score.CompetencyId, score => score.Score);
        var stored = await context.Set<PromotionFinalScore>().Where(score => score.ApplicationId == applicationId).ToListAsync(cancellationToken);
        context.RemoveRange(stored.Where(score => !given.ContainsKey(score.CompetencyId)));
        foreach (var score in stored.Where(score => given.ContainsKey(score.CompetencyId)))
        {
            score.Score = given[score.CompetencyId];
        }

        context.AddRange(given
            .Where(pair => stored.All(score => score.CompetencyId != pair.Key))
            .Select(pair => new PromotionFinalScore { ApplicationId = applicationId, CompetencyId = pair.Key, Score = pair.Value }));
    }
}
