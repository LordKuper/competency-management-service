using Kalibr.OrgStructure;
using Kalibr.Platform;
using Kalibr.UserManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Kalibr.CareerFramework;

/// <summary>
/// The API that tells a global administrator what a unit has and lacks to work. It lives in this module, because only this module sees
/// the HR assignments, the org structure and the accounts together. The statuses follow the rules the module acts by: the organization
/// snapshot, the nearest HR assignment and whether an account can act, as an HR too.
/// </summary>
internal static class OrgUnitSetupEndpoints
{
    private const string Route = "/api/v1/career/org-units/{id:guid}/setup";
    private const string Tag = "Career";

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapGet(Route, GetAsync)
        .WithTags(Tag)
        .WithName("GetOrgUnitSetup")
        .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<UnitSetupResponse>, NotFound>> GetAsync(
        Guid id,
        IEmployeeDirectory employees,
        IUserDirectory users,
        ProcessRoles roles,
        CancellationToken cancellationToken)
    {
        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        if (!snapshot.Units.TryGetValue(id, out var unit))
        {
            return TypedResults.NotFound();
        }

        var head = unit.HeadEmployeeId is { } headId && await employees.FindAsync(headId, cancellationToken) is { IsActive: true } employee
            ? employee
            : null;
        var headAccount = head is null
            ? null
            : UnitAccountResponse.Of((await users.FindByEmployeeIdsAsync([head.Id], cancellationToken)).GetValueOrDefault(head.Id), forHr: false);

        var ancestry = snapshot.Ancestry(id);
        var assigneeOf = await roles.AssigneesAsync([.. ancestry.Select(candidate => candidate.Id)], cancellationToken);
        var hr = await OrgUnitHrEndpoints.DescribeAsync(ProcessRoles.NearestAssigned(ancestry, assigneeOf), assigneeOf, employees, users, cancellationToken);
        var hasHrCandidates = hr is not null || await users.HasHrCandidateAsync(cancellationToken);

        var ownIds = snapshot.UnitPositionIds[id].ToHashSet();
        var positions = new UnitPositionsResponse(
            PositionsOf(ownIds, snapshot),
            PositionsOf(snapshot.AvailablePositionIds(id).Except(ownIds), snapshot));

        return TypedResults.Ok(new UnitSetupResponse(
            [.. ancestry.Reverse().Select(candidate => new CareerUnitResponse(candidate.Id, candidate.Name))],
            snapshot.WorkingEmployeeCounts.Where(pair => pair.Key.OrgUnitId == id).Sum(pair => pair.Value),
            head is null ? null : new CareerEmployeeResponse(head.Id, head.FullName),
            headAccount,
            hr,
            positions,
            Rows(headAccount, hr?.Account, hasHrCandidates, positions)));
    }

    /// <summary>
    /// The status of each thing the unit needs. A head's or HR's account waits for the head or HR; an assigned HR whose account cannot act
    /// as an HR leaves the unit without an HR; a unit without an assignment tells whether anyone could be assigned; positions are missing
    /// when neither the unit nor any unit above it lists one, whether or not anyone works there.
    /// </summary>
    private static List<UnitSetupRowResponse> Rows(
        UnitAccountResponse? headAccount,
        UnitAccountResponse? hrAccount,
        bool hasHrCandidates,
        UnitPositionsResponse positions) =>
    [
        headAccount is null
            ? new(UnitSetupRow.Head, UnitSetupStatus.Missing, UnitSetupReason.NoHead)
            : new(UnitSetupRow.Head, UnitSetupStatus.Ready),
        AccountRow(UnitSetupRow.HeadAccount, headAccount, UnitSetupReason.NoHead),
        hrAccount switch
        {
            null => new(UnitSetupRow.Hr, UnitSetupStatus.Missing, hasHrCandidates ? UnitSetupReason.NoHr : UnitSetupReason.NoHrCandidates),
            { State: UnitAccountState.Active } => new(UnitSetupRow.Hr, UnitSetupStatus.Ready),
            _ => new(UnitSetupRow.Hr, UnitSetupStatus.Missing, UnitSetupReason.HrCannotAct),
        },
        AccountRow(UnitSetupRow.HrAccount, hrAccount, UnitSetupReason.NoHr),
        positions.Own.Count + positions.Inherited.Count == 0
            ? new(UnitSetupRow.Positions, UnitSetupStatus.Missing, UnitSetupReason.NoPositions)
            : new(UnitSetupRow.Positions, UnitSetupStatus.Ready),
    ];

    private static UnitSetupRowResponse AccountRow(UnitSetupRow row, UnitAccountResponse? account, UnitSetupReason waitingFor) => account?.State switch
    {
        null => new(row, UnitSetupStatus.Waiting, waitingFor),
        UnitAccountState.Active => new(row, UnitSetupStatus.Ready),
        UnitAccountState.None => new(row, UnitSetupStatus.Missing, UnitSetupReason.NoAccount),
        UnitAccountState.Invited => new(row, UnitSetupStatus.Missing, UnitSetupReason.Invited),
        UnitAccountState.NoHrRole => new(row, UnitSetupStatus.Missing, UnitSetupReason.NoHrRole),
        _ => new(row, UnitSetupStatus.Missing, UnitSetupReason.Blocked),
    };

    private static List<UnitPositionResponse> PositionsOf(IEnumerable<Guid> positionIds, OrgSnapshot snapshot) =>
    [
        .. positionIds
            .Select(snapshot.Positions.GetValueOrDefault)
            .OfType<PositionStatus>()
            .OrderBy(position => position.Name, TextCollation.Comparer)
            .ThenBy(position => position.Id)
            .Select(position => new UnitPositionResponse(position.Id, position.Name, position.IsActive)),
    ];
}
