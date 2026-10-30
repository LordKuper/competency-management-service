using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// The wave API. Global administrators and HR announce waves, within the units HR answer for, and read the waves they manage; the author and
/// global administrators change a wave, with <c>If-Match</c>. Every change of a wave takes the exclusive promotion lock, because the units of
/// unfinished waves must not repeat. Any signed-in user reads the open waves. A wave its reader may not see is reported as not found.
/// </summary>
internal static class WaveEndpoints
{
    private const string Route = "/api/v1/promotion/waves";
    private const string Tag = "Promotion";
    private const string UnitsField = "unitIds";
    private const string FinishedMessage = "Карьерная сессия завершена: её нельзя изменить";
    private const string UnitsFixedMessage = "После начала карьерной сессии подразделения менять нельзя";
    private const string NoUnitsMessage = "Выберите хотя бы одно подразделение";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/open", ListOpenAsync).WithName("ListOpenPromotionWaves")
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        var managers = group.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        managers.MapGet(string.Empty, ListAsync).WithName("ListPromotionWaves");
        managers.MapPost(string.Empty, CreateAsync).WithName("CreatePromotionWave").ProducesETag();
        managers.MapGet("/{id:guid}", GetAsync).WithName("GetPromotionWave").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        managers.MapPut("/{id:guid}", UpdateAsync).WithName("UpdatePromotionWave").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Ok<IReadOnlyList<OpenWaveResponse>>> ListOpenAsync(OpenWaves open, CancellationToken cancellationToken) =>
        TypedResults.Ok(await open.ReadAsync(cancellationToken));

    private static async Task<Results<Ok<PageResponse<WaveResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] WaveListQuery query,
        AppDbContext context,
        WaveReader reader,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var page = await (await reader.VisibleAsync(context.Set<PromotionWave>().AsNoTracking(), cancellationToken))
            .OrderByDescending(wave => wave.StartsOn)
            .ThenBy(wave => wave.Id)
            .ToPageAsync(query.Page, query.PageSize, cancellationToken);
        return TypedResults.Ok(new PageResponse<WaveResponse>(await reader.DescribeAsync([.. page.Items], cancellationToken), page.Total, page.Page, page.PageSize));
    }

    private static async Task<Results<Ok<WaveResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        WaveReader reader,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var wave = await context.Set<PromotionWave>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (wave is null || !await reader.CanSeeAsync(wave, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        response.SetETag(wave.Version);
        return TypedResults.Ok((await reader.DescribeAsync([wave], cancellationToken))[0]);
    }

    /// <summary>
    /// Announces a wave. Its first stage starts at least a week from today and none of its stages ends in the past, each unit exists, works, is one the
    /// author answers for, belongs to no unfinished wave and has a commission with a chair. The employees of its units are told by letter once it is saved.
    /// </summary>
    private static async Task<Results<Created<WaveResponse>, ValidationProblem>> CreateAsync(
        WaveRequest request,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        WaveUnits waveUnits,
        WaveReader reader,
        PromotionMail mail,
        ICurrentActor actor,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if (request.UnitIds is not { Length: > 0 })
        {
            return Rejection.Invalid(UnitsField, NoUnitsMessage);
        }

        await using var transaction = await locks.BeginExclusiveAsync(cancellationToken);
        var errors = WaveDates.Validate(request, wave: null, stage: 0, announcedOn: calendar.Today, calendar.Today);
        var unitIds = request.UnitIds.Distinct().ToList();
        if (await waveUnits.ProblemsAsync(unitIds, waveId: null, cancellationToken) is { Count: > 0 } problems)
        {
            errors[UnitsField] = [.. problems];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var wave = new PromotionWave { AuthorId = actor.UserId!.Value, StartsOn = request.StartsOn, EndsOn = [.. request.EndsOn] };
        context.Add(wave);
        context.AddRange(unitIds.Select(unitId => new PromotionWaveUnit { WaveId = wave.Id, OrgUnitId = unitId }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await mail.WaveAnnouncedAsync(wave, cancellationToken);

        response.SetETag(wave.Version);
        return TypedResults.Created($"{Route}/{wave.Id}", (await reader.DescribeAsync([wave], cancellationToken))[0]);
    }

    /// <summary>
    /// Changes the dates and units of a wave. Before it starts everything may change; afterwards the start date, the units and the end dates
    /// of finished stages stay, and the end dates of the running and later stages move freely, past days included. A wave that is over does not change.
    /// </summary>
    private static async Task<Results<Ok<WaveResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        WaveRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        PromotionCalendar calendar,
        WaveUnits waveUnits,
        WaveReader reader,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if (request.UnitIds is not { Length: > 0 })
        {
            return Rejection.Invalid(UnitsField, NoUnitsMessage);
        }

        await using var transaction = await locks.BeginExclusiveAsync(cancellationToken);
        if (await locks.FindWaveForUpdateAsync(id, cancellationToken) is not { } wave || !await reader.CanSeeAsync(wave, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        if (!reader.CanEdit(wave))
        {
            return Rejection.Forbidden();
        }

        var stage = calendar.StageOf(wave);
        if (wave.IsFinished || stage == PromotionCalendar.AfterLastStage)
        {
            return Rejection.Conflict(FinishedMessage);
        }

        ifMatch.ApplyTo(context, wave);
        var errors = WaveDates.Validate(request, wave, stage, calendar.DayOf(wave.CreatedAt), calendar.Today);
        var stored = await context.Set<PromotionWaveUnit>().Where(member => member.WaveId == id).ToListAsync(cancellationToken);
        var unitIds = request.UnitIds.Distinct().ToList();
        var added = unitIds.Except(stored.Select(member => member.OrgUnitId)).ToList();
        if (stage >= 1)
        {
            if (added.Count > 0 || stored.Any(member => !unitIds.Contains(member.OrgUnitId)))
            {
                errors[UnitsField] = [UnitsFixedMessage];
            }
        }
        else if (await waveUnits.ProblemsAsync(added, id, cancellationToken) is { Count: > 0 } problems)
        {
            errors[UnitsField] = [.. problems];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        wave.StartsOn = request.StartsOn;
        wave.EndsOn = [.. request.EndsOn];
        context.Entry(wave).Property(entry => entry.Version).IsModified = true;
        context.RemoveRange(stored.Where(member => !unitIds.Contains(member.OrgUnitId)));
        context.AddRange(added.Select(unitId => new PromotionWaveUnit { WaveId = id, OrgUnitId = unitId }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.SetETag(wave.Version);
        return TypedResults.Ok((await reader.DescribeAsync([wave], cancellationToken))[0]);
    }
}
