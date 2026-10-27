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
/// What a wave offers about applications. Any signed-in user reads the form of a target of a wave that takes applications. The applications of
/// a wave are listed for those who run it, or have a role in the unit of the target: its author and global administrators see all of them, the
/// responsible HR, the head and the members of a commission only those aimed at their own unit. A wave nobody may list is reported as not found.
/// </summary>
internal static class WaveApplicationEndpoints
{
    private const string Route = "/api/v1/promotion/waves/{waveId:guid}";
    private const string Tag = "Promotion";
    private const int SubmissionStage = 1;

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/applications", ListAsync).WithName("ListPromotionWaveApplications");
        group.MapGet("/targets/{matrixId:guid}", GetTargetAsync).WithName("GetPromotionTargetForm");
    }

    private static async Task<Results<Ok<PageResponse<ApplicationSummaryResponse>>, NotFound, ValidationProblem>> ListAsync(
        Guid waveId,
        [AsParameters] WaveListQuery query,
        AppDbContext context,
        ApplicationAccess access,
        ApplicationReader reader,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var wave = await context.Set<PromotionWave>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == waveId, cancellationToken);
        if (wave is null)
        {
            return TypedResults.NotFound();
        }

        var unitIds = await context.Set<PromotionWaveUnit>().AsNoTracking().Where(member => member.WaveId == waveId).Select(member => member.OrgUnitId).ToListAsync(cancellationToken);
        var viewer = await access.ViewerAsync(wave, unitIds, cancellationToken);
        var applications = context.Set<PromotionApplication>().AsNoTracking().Where(application => application.WaveId == waveId);
        if (viewer.WaveRoles == ApplicationRole.None)
        {
            var visibleUnitIds = viewer.UnitRoles.Where(unit => unit.Value != ApplicationRole.None).Select(unit => unit.Key).ToArray();
            if (visibleUnitIds.Length == 0)
            {
                return TypedResults.NotFound();
            }

            applications = applications.Where(application => visibleUnitIds.Contains(application.OrgUnitId));
        }

        var page = await applications.OrderBy(application => application.CreatedAt).ThenBy(application => application.Id).ToPageAsync(query.Page, query.PageSize, cancellationToken);
        var items = await reader.SummariesAsync([.. page.Items], new Dictionary<Guid, PromotionWave> { [wave.Id] = wave }, viewer, cancellationToken);
        return TypedResults.Ok(new PageResponse<ApplicationSummaryResponse>(items, page.Total, page.Page, page.PageSize));
    }

    private static async Task<Results<Ok<TargetFormResponse>, NotFound>> GetTargetAsync(
        Guid waveId,
        Guid matrixId,
        AppDbContext context,
        IEmployeeDirectory employees,
        ICareerModel career,
        PromotionCalendar calendar,
        CancellationToken cancellationToken)
    {
        var wave = await context.Set<PromotionWave>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == waveId, cancellationToken);
        var matrix = await context.Set<PromotionWaveMatrix>().AsNoTracking().FirstOrDefaultAsync(bound => bound.WaveId == waveId && bound.MatrixId == matrixId, cancellationToken);
        if (wave is null || matrix is null || calendar.StageOf(wave) != SubmissionStage
            || await career.VersionAsync(matrix.VersionId, cancellationToken) is not { } version
            || await employees.FindUnitAsync(matrix.OrgUnitId, cancellationToken) is not { } unit)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new TargetFormResponse(
            matrix.MatrixId,
            unit.Id,
            unit.Name,
            matrix.TrackName,
            [.. version.Levels.OrderBy(level => level.Order).Select(level => new TargetFormLevelResponse(level.Id, level.Name, level.RequiredEducation))],
            [.. version.Competencies.Select(competency => new TargetFormCompetencyResponse(competency.Id, competency.Name, competency.ScalePoints))]));
    }
}
