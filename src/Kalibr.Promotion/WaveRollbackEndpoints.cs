using Kalibr.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.Promotion;

/// <summary>
/// Going back to a stage of a wave that has passed. Those who change the dates of a wave, its author and global administrators, ask what the return
/// would reset and then make it, with <c>If-Match</c>; a wave that is over cannot be taken back. A wave its reader may not see is reported as not found.
/// </summary>
internal static class WaveRollbackEndpoints
{
    private const string Route = "/api/v1/promotion/waves/{id:guid}/rollback";
    private const string Tag = "Promotion";
    private const string StageField = "stage";
    private const string FinishedMessage = "Карьерная сессия завершена: её нельзя откатить";
    private const string StageMessage = "Выберите этап, который уже прошёл";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, PreviewAsync).WithName("PreviewPromotionWaveRollback");
        group.MapPost(string.Empty, RollbackAsync).WithName("RollbackPromotionWave").ProducesETag();
    }

    /// <summary>
    /// Tells what going back to a stage would reset or compute again, so the user can confirm it.
    /// </summary>
    private static async Task<Results<Ok<RollbackPreviewResponse>, NotFound, ValidationProblem, ProblemHttpResult>> PreviewAsync(
        Guid id,
        int stage,
        AppDbContext context,
        WaveReader reader,
        WaveRollback rollback,
        PromotionCalendar calendar,
        CancellationToken cancellationToken)
    {
        var wave = await context.Set<PromotionWave>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (wave is null || !await reader.CanSeeAsync(wave, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        if (!reader.CanEdit(wave))
        {
            return Rejection.Forbidden();
        }

        var current = calendar.StageOf(wave);
        if (wave.IsFinished || current == PromotionCalendar.AfterLastStage)
        {
            return Rejection.Conflict(FinishedMessage);
        }

        return stage < 1 || stage >= current
            ? Rejection.Invalid(StageField, StageMessage)
            : TypedResults.Ok(await rollback.PreviewAsync(wave, stage, cancellationToken));
    }

    /// <summary>
    /// Takes an unfinished wave back to a stage that has passed: the stage becomes the current one today, with the new end dates of it and the
    /// stages after it, and the end actions of those stages are carried out again when they end.
    /// </summary>
    private static async Task<Results<Ok<WaveResponse>, NotFound, ValidationProblem, ProblemHttpResult>> RollbackAsync(
        Guid id,
        RollbackRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        WaveReader reader,
        WaveRollback rollback,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await locks.BeginSharedAsync(cancellationToken);
        if (await locks.FindWaveForUpdateAsync(id, cancellationToken) is not { } wave || !await reader.CanSeeAsync(wave, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        if (!reader.CanEdit(wave))
        {
            return Rejection.Forbidden();
        }

        var current = calendar.StageOf(wave);
        if (wave.IsFinished || current == PromotionCalendar.AfterLastStage)
        {
            return Rejection.Conflict(FinishedMessage);
        }

        if (request.Stage < 1 || request.Stage >= current)
        {
            return Rejection.Invalid(StageField, StageMessage);
        }

        ifMatch.ApplyTo(context, wave);
        if (WaveDates.ValidateRollback(request, wave, calendar.Today) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await rollback.ApplyAsync(wave, request, cancellationToken);
        context.Entry(wave).Property(entry => entry.Version).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(wave.Version);
        return TypedResults.Ok((await reader.DescribeAsync([wave], cancellationToken))[0]);
    }
}
