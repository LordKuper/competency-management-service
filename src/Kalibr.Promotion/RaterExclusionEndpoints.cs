using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// The list of employees and positions that cannot be named as raters. Only global administrators keep it; anyone signed in may ask which of some
/// employees it excludes, because the rater search marks them.
/// </summary>
internal static class RaterExclusionEndpoints
{
    private const string Route = "/api/v1/promotion";
    private const string Tag = "Promotion";
    private const int MaxLookup = 100;
    private const string TooManyMessage = "Спросите не больше ста сотрудников за раз";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/rater-exclusions", FindAmongAsync).WithName("FindExcludedPromotionRaters").RequireAuthorization(AuthorizationPolicies.Authenticated);

        var admin = group.MapGroup("/settings/rater-exclusions")
            .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        admin.MapGet(string.Empty, ListAsync).WithName("ListPromotionRaterExclusions");
        admin.MapPut("/employees/{employeeId:guid}", AddEmployeeAsync).WithName("AddPromotionRaterExclusionEmployee").ProducesProblem(StatusCodes.Status404NotFound);
        admin.MapDelete("/employees/{employeeId:guid}", RemoveEmployeeAsync).WithName("RemovePromotionRaterExclusionEmployee");
        admin.MapPut("/positions/{positionId:guid}", AddPositionAsync).WithName("AddPromotionRaterExclusionPosition").ProducesProblem(StatusCodes.Status404NotFound);
        admin.MapDelete("/positions/{positionId:guid}", RemovePositionAsync).WithName("RemovePromotionRaterExclusionPosition");
    }

    /// <summary>
    /// Says which of the asked employees are excluded, by themselves or by their position.
    /// </summary>
    private static async Task<Results<Ok<IReadOnlyList<Guid>>, ValidationProblem>> FindAmongAsync(
        [FromQuery] Guid[] employeeIds,
        IEmployeeDirectory employees,
        RaterExclusions exclusions,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Length > MaxLookup)
        {
            return Rejection.Invalid(nameof(employeeIds), TooManyMessage);
        }

        var people = await employees.FindManyAsync(employeeIds, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<Guid>>([.. await exclusions.AmongAsync(people.Values, cancellationToken)]);
    }

    /// <summary>
    /// Lists the excluded employees with their units and the excluded positions with the number of working employees who hold them.
    /// </summary>
    private static async Task<Ok<RaterExclusionsResponse>> ListAsync(AppDbContext context, IEmployeeDirectory employees, CancellationToken cancellationToken)
    {
        var rows = await context.Set<PromotionRaterExclusion>().AsNoTracking().ToListAsync(cancellationToken);
        var people = await employees.FindManyAsync([.. rows.Where(row => row.Kind == RaterExclusionKind.Employee).Select(row => row.SubjectId)], cancellationToken);
        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        return TypedResults.Ok(new RaterExclusionsResponse(
            [.. people.Values
                .OrderBy(person => person.FullName, TextCollation.Comparer)
                .ThenBy(person => person.Id)
                .Select(person => new ExcludedEmployeeResponse(person.Id, person.FullName, snapshot.Units[person.OrgUnitId].Name))],
            [.. rows.Where(row => row.Kind == RaterExclusionKind.Position && snapshot.Positions.ContainsKey(row.SubjectId))
                .Select(row => snapshot.Positions[row.SubjectId])
                .OrderBy(position => position.Name, TextCollation.Comparer)
                .ThenBy(position => position.Id)
                .Select(position => new ExcludedPositionResponse(
                    position.Id,
                    position.Name,
                    snapshot.WorkingEmployeeCounts.Where(count => count.Key.PositionId == position.Id).Sum(count => count.Value)))]));
    }

    private static async Task<Results<NoContent, NotFound>> AddEmployeeAsync(Guid employeeId, AppDbContext context, IEmployeeDirectory employees, CancellationToken cancellationToken) =>
        await AddAsync(RaterExclusionKind.Employee, employeeId, await employees.FindAsync(employeeId, cancellationToken) is not null, context, cancellationToken);

    private static async Task<Results<NoContent, NotFound>> AddPositionAsync(Guid positionId, AppDbContext context, IEmployeeDirectory employees, CancellationToken cancellationToken) =>
        await AddAsync(RaterExclusionKind.Position, positionId, (await employees.FindOrgSnapshotAsync(cancellationToken)).Positions.ContainsKey(positionId), context, cancellationToken);

    private static Task<NoContent> RemoveEmployeeAsync(Guid employeeId, AppDbContext context, CancellationToken cancellationToken) =>
        RemoveAsync(RaterExclusionKind.Employee, employeeId, context, cancellationToken);

    private static Task<NoContent> RemovePositionAsync(Guid positionId, AppDbContext context, CancellationToken cancellationToken) =>
        RemoveAsync(RaterExclusionKind.Position, positionId, context, cancellationToken);

    private static async Task<Results<NoContent, NotFound>> AddAsync(RaterExclusionKind kind, Guid subjectId, bool exists, AppDbContext context, CancellationToken cancellationToken)
    {
        if (!exists)
        {
            return TypedResults.NotFound();
        }

        if (!await context.Set<PromotionRaterExclusion>().AnyAsync(row => row.Kind == kind && row.SubjectId == subjectId, cancellationToken))
        {
            context.Add(new PromotionRaterExclusion { Kind = kind, SubjectId = subjectId });
            await context.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RemoveAsync(RaterExclusionKind kind, Guid subjectId, AppDbContext context, CancellationToken cancellationToken)
    {
        context.RemoveRange(await context.Set<PromotionRaterExclusion>().Where(row => row.Kind == kind && row.SubjectId == subjectId).ToListAsync(cancellationToken));
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}
