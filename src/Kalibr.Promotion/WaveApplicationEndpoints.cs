using Kalibr.CareerFramework;
using Kalibr.OrgStructure;
using Kalibr.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.Promotion;

/// <summary>
/// What a wave offers about applications. A signed-in employee who can file applications reads the form of a target of a wave that takes them, with
/// only the levels they may aim at and the education on their card. The applications of
/// a wave are listed for those who run it, or have a role in the unit of the target: its author and global administrators see all of them, the
/// responsible HR, the head and the members of a commission only those aimed at their own unit. A wave nobody may list is reported as not found.
/// </summary>
internal static class WaveApplicationEndpoints
{
    private const string Route = "/api/v1/promotion/waves/{waveId:guid}";
    private const string Tag = "Promotion";
    private const int SubmissionStage = 1;

    private static readonly ApplicationStatus[] HeadStatuses =
        [ApplicationStatus.Submitted, ApplicationStatus.Admitted, ApplicationStatus.UnderReview, ApplicationStatus.Decided, ApplicationStatus.NoDecision];

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/applications", ListAsync).WithName("ListPromotionWaveApplications");
        group.MapGet("/session", GetSessionAsync).WithName("GetPromotionSession");
        group.MapGet("/head/candidates", ListHeadCandidatesAsync).WithName("ListPromotionHeadCandidates");
        group.MapGet("/targets/{unitId:guid}/{matrixId:guid}", GetTargetAsync).WithName("GetPromotionTargetForm").ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok<ApplicationPageResponse>, NotFound, ValidationProblem>> ListAsync(
        Guid waveId,
        [AsParameters] ApplicationListQuery query,
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

        var seen = (await applications.OrderBy(application => application.CreatedAt).ThenBy(application => application.Id).ToListAsync(cancellationToken))
            .Where(application => viewer.RolesOf(application) != ApplicationRole.None)
            .ToList();
        var rows = await reader.SummariesAsync(seen, new Dictionary<Guid, PromotionWave> { [wave.Id] = wave }, viewer, cancellationToken);
        var search = query.Search?.Trim();
        var searched = string.IsNullOrEmpty(search) ? rows : [.. rows.Where(row => row.CandidateName.Contains(search, StringComparison.OrdinalIgnoreCase))];
        var counts = new ApplicationCountsResponse(
            searched.Count,
            searched.Count(row => row.NeedsFormalData),
            [.. searched.GroupBy(row => row.Status).OrderBy(group => group.Key).Select(group => new ApplicationStatusCountResponse(group.Key, group.Count()))]);
        var matching = searched.Where(row => (query.Status is null || row.Status == query.Status) && (query.NeedsFormalData != true || row.NeedsFormalData)).ToList();
        return TypedResults.Ok(new ApplicationPageResponse(
            [.. matching.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)],
            matching.Count,
            query.Page,
            query.PageSize,
            counts));
    }

    /// <summary>
    /// Reads the dates of a session for everyone who has a part in it: a manager of the wave, a member of a commission of its units, a candidate or a rater.
    /// </summary>
    private static async Task<Results<Ok<SessionDatesResponse>, NotFound>> GetSessionAsync(
        Guid waveId,
        AppDbContext context,
        ApplicationAccess access,
        PromotionCalendar calendar,
        CancellationToken cancellationToken)
    {
        var wave = await context.Set<PromotionWave>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == waveId, cancellationToken);
        if (wave is null)
        {
            return TypedResults.NotFound();
        }

        var unitIds = await context.Set<PromotionWaveUnit>().AsNoTracking().Where(member => member.WaveId == waveId).Select(member => member.OrgUnitId).ToListAsync(cancellationToken);
        var viewer = await access.ViewerAsync(wave, unitIds, cancellationToken);
        var hasPart = viewer.WaveRoles != ApplicationRole.None
            || viewer.UnitRoles.Any(unit => unit.Value != ApplicationRole.None)
            || (viewer.EmployeeId is { } employeeId
                && await context.Set<PromotionApplication>().AnyAsync(
                    application => application.WaveId == waveId
                        && (application.EmployeeId == employeeId
                            || context.Set<PromotionRater>().Any(rater => rater.ApplicationId == application.Id && rater.EmployeeId == employeeId)),
                    cancellationToken));
        return hasPart ? TypedResults.Ok(SessionDatesResponse.From(wave, calendar.StageOf(wave))) : TypedResults.NotFound();
    }

    /// <summary>
    /// Lists the candidates of the units the signed-in user heads in a wave, with how complete the lists of their raters are; the units of a head are the
    /// ones the commission roster names. Drafts, withdrawn and formally rejected applications are left out.
    /// </summary>
    private static async Task<Results<Ok<HeadCandidatesResponse>, NotFound>> ListHeadCandidatesAsync(
        Guid waveId,
        AppDbContext context,
        ApplicationAccess access,
        ApplicationReader reader,
        PromotionCalendar calendar,
        CancellationToken cancellationToken)
    {
        var wave = await context.Set<PromotionWave>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == waveId, cancellationToken);
        if (wave is null)
        {
            return TypedResults.NotFound();
        }

        var unitIds = await context.Set<PromotionWaveUnit>().AsNoTracking().Where(member => member.WaveId == waveId).Select(member => member.OrgUnitId).ToListAsync(cancellationToken);
        var viewer = await access.ViewerAsync(wave, unitIds, cancellationToken);
        var headed = viewer.UnitRoles.Where(unit => unit.Value.HasFlag(ApplicationRole.Head)).Select(unit => unit.Key).ToArray();
        if (headed.Length == 0)
        {
            return TypedResults.NotFound();
        }

        var applications = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == waveId && headed.Contains(application.OrgUnitId) && HeadStatuses.Contains(application.Status))
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(new HeadCandidatesResponse(
            SessionDatesResponse.From(wave, calendar.StageOf(wave)),
            await reader.HeadCandidatesAsync(applications, wave, cancellationToken)));
    }

    private static async Task<Results<Ok<TargetFormResponse>, NotFound, ProblemHttpResult>> GetTargetAsync(
        Guid waveId,
        Guid unitId,
        Guid matrixId,
        AppDbContext context,
        IEmployeeDirectory employees,
        ICareerModel career,
        ApplicationAccess access,
        ApplicationRules rules,
        PromotionCalendar calendar,
        CancellationToken cancellationToken)
    {
        var wave = await context.Set<PromotionWave>().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == waveId, cancellationToken);
        var matrix = await context.Set<PromotionWaveMatrix>().AsNoTracking().FirstOrDefaultAsync(bound => bound.WaveId == waveId && bound.OrgUnitId == unitId && bound.MatrixId == matrixId, cancellationToken);
        if (wave is null || matrix is null || calendar.StageOf(wave) != SubmissionStage
            || await career.VersionAsync(matrix.VersionId, cancellationToken) is not { } version
            || await employees.FindUnitAsync(matrix.OrgUnitId, cancellationToken) is not { } unit)
        {
            return TypedResults.NotFound();
        }

        if (await access.ActingCandidateAsync(cancellationToken) is not { } candidate)
        {
            return Rejection.Forbidden();
        }

        var allowed = await rules.AllowedLevelIdsAsync(matrix, version, candidate, cancellationToken);
        return TypedResults.Ok(new TargetFormResponse(
            matrix.MatrixId,
            unit.Id,
            unit.Name,
            matrix.TrackName,
            candidate.Education,
            [.. version.Levels.Where(level => allowed.Contains(level.Id)).OrderBy(level => level.Order).Select(level => new TargetFormLevelResponse(level.Id, level.Name, level.RequiredEducation))],
            [.. version.Competencies.Select(competency => TargetFormCompetencyResponse.Of(version, competency, withRequirements: true))]));
    }
}
