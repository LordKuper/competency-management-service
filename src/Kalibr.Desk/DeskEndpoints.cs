using Kalibr.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Kalibr.Desk;

/// <summary>
/// The API of the signed-in user's desk. It answers only about the user: no parameter names whose desk it is, so nobody can read another's.
/// </summary>
internal static class DeskEndpoints
{
    private const string Route = "/api/v1/desk";
    private const string Tag = "Desk";

    /// <summary>
    /// How many items of each group the desk shows at first.
    /// </summary>
    private const int FirstItems = 10;

    /// <summary>
    /// The ceiling of the items one request returns. Items past it are out of the desk's reach, not on a next page: items leave as they are
    /// done, so the client always reads a group from its start.
    /// </summary>
    private const int MaxItems = 2000;

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var desk = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        desk.MapGet(string.Empty, GetAsync).WithName("GetDesk");
        desk.MapGet("/items", ListItemsAsync).WithName("ListDeskItems");
        desk.MapGet("/count", CountAsync).WithName("GetDeskCount");
    }

    private static async Task<Ok<DeskResponse>> GetAsync(DeskBoard board, CancellationToken cancellationToken)
    {
        var (items, prerequisites) = await board.ReadAsync(cancellationToken);
        return TypedResults.Ok(new DeskResponse(
            ActionCount(items),
            [
                .. items
                    .GroupBy(item => item.Group)
                    .Select(group => new DeskGroupResponse(group.Key, group.First().IsReference, group.Count(), [.. group.Take(FirstItems)])),
            ],
            prerequisites));
    }

    /// <summary>
    /// The first items of the chosen groups, or of all groups when none is chosen.
    /// </summary>
    private static async Task<Results<Ok<DeskItemsResponse>, ValidationProblem>> ListItemsAsync(
        DeskBoard board,
        CancellationToken cancellationToken,
        [FromQuery(Name = "group")] DeskGroup[]? group = null,
        [FromQuery(Name = "limit")] int limit = FirstItems)
    {
        if (limit < 1)
        {
            return Rejection.Invalid("limit", "limit must be at least 1.");
        }

        if (group is not null && !group.All(Enum.IsDefined))
        {
            return Rejection.Invalid("group", "group must name a desk group.");
        }

        var items = (await board.ReadAsync(cancellationToken)).Items
            .Where(item => group is not { Length: > 0 } || group.Contains(item.Group))
            .ToList();
        return TypedResults.Ok(new DeskItemsResponse([.. items.Take(Math.Min(limit, MaxItems))], items.Count));
    }

    private static async Task<Ok<DeskCountResponse>> CountAsync(DeskBoard board, CancellationToken cancellationToken) =>
        TypedResults.Ok(new DeskCountResponse(ActionCount((await board.ReadAsync(cancellationToken)).Items)));

    private static int ActionCount(List<DeskItemResponse> items) => items.Count(item => !item.IsReference);
}
