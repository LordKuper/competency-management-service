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
/// The API of the commission of a unit. The responsible HR of the unit, a global administrator and the members read it; the HR and the
/// administrator change it, with <c>If-Match</c>, under the promotion locks. A commission that nobody can see is reported as not found.
/// </summary>
internal static class CommissionEndpoints
{
    private const string Route = "/api/v1/promotion/commissions/{unitId:guid}";
    private const string Tag = "Promotion";
    private const string MembersField = "memberEmployeeIds";
    private const string ChairField = "chairEmployeeId";
    private const string ChairRequiredMessage = "У подразделения незавершённой карьерной сессии должен быть председатель комиссии";
    private const string ChairExcludedMessage = "Председателя нельзя исключить из комиссии подразделения незавершённой карьерной сессии";
    private const string ChairNotMemberMessage = "Председатель должен быть членом комиссии, который работает и имеет действующую учётную запись";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/v1/promotion/commissions", ListAsync).WithName("ListPromotionCommissions")
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        group.MapGet(string.Empty, GetAsync).WithName("GetPromotionCommission").ProducesETag();
        group.MapPut(string.Empty, UpdateAsync).WithName("UpdatePromotionCommission").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Lists the commissions of the units the signed-in user answers for, all the working units for a global administrator, with the number of members and
    /// whether the unit has a chair.
    /// </summary>
    private static async Task<Ok<IReadOnlyList<CommissionSummaryResponse>>> ListAsync(
        CommissionReader reader,
        IEmployeeDirectory employees,
        ICareerModel career,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OrgUnitStatus> units = actor.IsGlobalAdmin()
            ? [.. (await employees.FindOrgSnapshotAsync(cancellationToken)).Units.Values.Where(unit => unit.IsActive)]
            : actor.UserId is { } userId ? await career.ResponsibleUnitsAsync(userId, cancellationToken) : [];
        var rosters = await reader.ReadAsync([.. units.Select(unit => unit.Id)], cancellationToken);
        return TypedResults.Ok<IReadOnlyList<CommissionSummaryResponse>>([.. units
            .Select(unit => rosters[unit.Id])
            .Select(roster => new CommissionSummaryResponse(
                roster.UnitId,
                roster.UnitName,
                roster.Members.Count,
                roster.ChairEmployeeId is not null,
                roster.Members.FirstOrDefault(member => member.EmployeeId == roster.ChairEmployeeId)?.FullName))
            .OrderBy(row => row.UnitName, TextCollation.Comparer)
            .ThenBy(row => row.UnitId)]);
    }

    private static async Task<Results<Ok<CommissionResponse>, NotFound>> GetAsync(
        Guid unitId,
        CommissionReader reader,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if ((await reader.ReadAsync([unitId], cancellationToken)).GetValueOrDefault(unitId) is not { } roster
            || await reader.RightsAsync(roster, cancellationToken) == CommissionRights.None)
        {
            return TypedResults.NotFound();
        }

        response.SetETag(roster.Version);
        return TypedResults.Ok(CommissionResponse.From(roster));
    }

    /// <summary>
    /// Replaces the members and the chair, at any stage of the wave. The chair must be a member who acts, and a unit of an unfinished wave keeps one: the chair cannot be
    /// taken off or left out of the members, but may be replaced, including one who no longer acts. The first change of a unit that has no stored commission sends version 0.
    /// Members added while applications are under review are told of them once the change has committed.
    /// </summary>
    private static async Task<Results<Ok<CommissionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid unitId,
        CommissionRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        PromotionLocks locks,
        CommissionReader reader,
        ActingEmployees acting,
        PromotionMail mail,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await locks.BeginSharedAsync(cancellationToken);
        if ((await reader.ReadAsync([unitId], cancellationToken)).GetValueOrDefault(unitId) is not { } roster)
        {
            return TypedResults.NotFound();
        }

        switch (await reader.RightsAsync(roster, cancellationToken))
        {
            case CommissionRights.None:
                return TypedResults.NotFound();
            case CommissionRights.Read:
                return Rejection.Forbidden();
        }

        var memberIds = request.MemberEmployeeIds.Distinct().ToList();
        if (await acting.ProblemsAsync(memberIds, cancellationToken) is { Count: > 0 } problems)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [MembersField] = [.. problems] });
        }

        if (await ChairProblemAsync(context, request.ChairEmployeeId, memberIds, roster, cancellationToken) is { } chairProblem)
        {
            return chairProblem.IsConflict ? Rejection.Conflict(chairProblem.Message) : Rejection.Invalid(ChairField, chairProblem.Message);
        }

        await SaveAsync(context, unitId, request.ChairEmployeeId, memberIds, ifMatch, cancellationToken);
        var saved = (await reader.ReadAsync([unitId], cancellationToken))[unitId];
        await transaction.CommitAsync(cancellationToken);
        await mail.MembersAddedAsync(unitId, roster.UnitName, [.. memberIds.Where(id => roster.Members.All(member => member.EmployeeId != id))], cancellationToken);
        response.SetETag(saved.Version);
        return TypedResults.Ok(CommissionResponse.From(saved));
    }

    /// <summary>
    /// Says why the chair cannot be the one the request names.
    /// </summary>
    /// <returns>The reason and whether it is a conflict with the unit being in an unfinished wave rather than an invalid field; null when the chair is fine.</returns>
    private static async Task<(string Message, bool IsConflict)?> ChairProblemAsync(
        AppDbContext context,
        Guid? chairId,
        IReadOnlyCollection<Guid> memberIds,
        CommissionRoster roster,
        CancellationToken cancellationToken)
    {
        var isKept = await context.Set<PromotionWaveUnit>()
            .AnyAsync(
                unit => unit.OrgUnitId == roster.UnitId
                    && context.Set<PromotionWave>().Any(wave => wave.Id == unit.WaveId && wave.ProcessedStage < PromotionWave.StageCount),
                cancellationToken);
        if (chairId is not { } chair)
        {
            return isKept ? (ChairRequiredMessage, true) : null;
        }

        var isMember = memberIds.Contains(chair) || roster.Members.Any(member => member.EmployeeId == chair && member.IsRequired && member.IsActing);
        if (isMember)
        {
            return null;
        }

        return isKept && chair == roster.ChairEmployeeId ? (ChairExcludedMessage, true) : (ChairNotMemberMessage, false);
    }

    private static async Task SaveAsync(
        AppDbContext context,
        Guid unitId,
        Guid? chairId,
        IReadOnlyCollection<Guid> memberIds,
        IfMatch ifMatch,
        CancellationToken cancellationToken)
    {
        var commission = await context.Set<PromotionCommission>().FirstOrDefaultAsync(candidate => candidate.OrgUnitId == unitId, cancellationToken);
        if (commission is null)
        {
            if (ifMatch.Version != 0)
            {
                throw new DbUpdateConcurrencyException("The If-Match version is not the current version.");
            }

            commission = new PromotionCommission { OrgUnitId = unitId };
            context.Add(commission);
        }
        else
        {
            ifMatch.ApplyTo(context, commission);
            context.Entry(commission).Property(entry => entry.Version).IsModified = true;
        }

        commission.ChairEmployeeId = chairId;
        var stored = await context.Set<PromotionCommissionMember>().Where(member => member.OrgUnitId == unitId).ToListAsync(cancellationToken);
        context.RemoveRange(stored.Where(member => !memberIds.Contains(member.EmployeeId)));
        context.AddRange(memberIds
            .Where(employeeId => stored.All(member => member.EmployeeId != employeeId))
            .Select(employeeId => new PromotionCommissionMember { OrgUnitId = unitId, EmployeeId = employeeId }));
        await context.SaveChangesAsync(cancellationToken);
    }
}
