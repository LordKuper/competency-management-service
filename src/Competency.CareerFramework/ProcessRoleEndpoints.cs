using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// The API that tells the signed-in user their own career process roles.
/// </summary>
internal static class ProcessRoleEndpoints
{
    private const string Route = "/api/v1/career/me";
    private const string Tag = "Career";

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapGet(Route, GetAsync)
        .WithTags(Tag)
        .WithName("GetCareerRoles")
        .RequireAuthorization(AuthorizationPolicies.Authenticated)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<Results<Ok<ProcessRolesResponse>, UnauthorizedHttpResult>> GetAsync(
        ICurrentActor actor,
        ProcessRoles roles,
        CancellationToken cancellationToken)
    {
        if (actor.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var headedUnits = await roles.HeadedUnitsAsync(userId, cancellationToken);
        var hrUnits = await roles.HrUnitsAsync(userId, cancellationToken);
        return TypedResults.Ok(new ProcessRolesResponse(ToResponse(headedUnits), ToResponse(hrUnits)));
    }

    private static List<CareerUnitResponse> ToResponse(IReadOnlyList<OrgUnitStatus> units) =>
        [.. units.Select(unit => new CareerUnitResponse(unit.Id, unit.Name))];
}
