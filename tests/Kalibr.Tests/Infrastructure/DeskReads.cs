using System.Net;
using System.Text.Json.Nodes;

namespace Kalibr.Tests.Infrastructure;

/// <summary>
/// Reading the desk of a signed-in user the way the client does, and picking items out of it by the object they are about.
/// </summary>
public static class DeskReads
{
    public const string DeskRoute = "/api/v1/desk";
    public const string CeilingQuery = "?limit=2000";

    private const string CommissionType = "PromotionCommission";

    /// <summary>
    /// The desk answer: the counter, the non-empty groups with their first items and the prerequisites that hold.
    /// </summary>
    public static async Task<JsonNode> DeskAsync(this ApiClient client) => (await client.GetAsync(DeskRoute)).Expect(HttpStatusCode.OK).Json!;

    /// <summary>
    /// Every item of the user's desk, up to the desk's ceiling, in desk order.
    /// </summary>
    public static async Task<List<JsonNode>> ItemsAsync(this ApiClient client, string? group = null)
    {
        var query = group is null ? CeilingQuery : $"{CeilingQuery}&group={group}";
        return [.. (await client.GetAsync($"{DeskRoute}/items{query}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray().Select(item => item!)];
    }

    /// <summary>
    /// The counter the header shows.
    /// </summary>
    public static async Task<int> CountAsync(this ApiClient client) =>
        (await client.GetAsync($"{DeskRoute}/count")).Expect(HttpStatusCode.OK).Json!["actionCount"]!.GetValue<int>();

    /// <summary>
    /// The item about the object, or null when the desk has none. The commission of a unit is an object of its own, though it takes the unit's id.
    /// </summary>
    public static JsonNode? ItemOf(this IEnumerable<JsonNode> items, Guid objectId) =>
        items.SingleOrDefault(item => item["object"]!["id"]!.GetValue<Guid>() == objectId && item["object"]!["type"]!.GetValue<string>() != CommissionType);

    /// <summary>
    /// The item about the commission of the unit, or null when the desk has none.
    /// </summary>
    public static JsonNode? CommissionOf(this IEnumerable<JsonNode> items, Guid unitId) =>
        items.SingleOrDefault(item => item["object"]!["id"]!.GetValue<Guid>() == unitId && item["object"]!["type"]!.GetValue<string>() == CommissionType);

    /// <summary>
    /// The item about the object, which the desk must have.
    /// </summary>
    public static JsonNode ItemFor(this IEnumerable<JsonNode> items, Guid objectId)
    {
        var list = items.ToList();
        return list.ItemOf(objectId) ?? throw new Xunit.Sdk.XunitException(
            $"The desk has no item about {objectId}; it has {list.Count} item(s): {string.Join(", ", list.Select(item => $"{item["object"]!["type"]} {item["object"]!["name"]} [{string.Join('/', item.Kinds())}]"))}");
    }

    /// <summary>
    /// The kinds of the item's reasons, the most important first.
    /// </summary>
    public static string[] Kinds(this JsonNode item) => [.. item["reasons"]!.AsArray().Select(reason => reason!["kind"]!.GetValue<string>())];

    /// <summary>
    /// The item's first reason, which decides where its link leads.
    /// </summary>
    public static JsonNode FirstReason(this JsonNode item) => item["reasons"]![0]!;

    /// <summary>
    /// The item's reason of the given kind, or null.
    /// </summary>
    public static JsonNode? ReasonOf(this JsonNode item, string kind) =>
        item["reasons"]!.AsArray().SingleOrDefault(reason => reason!["kind"]!.GetValue<string>() == kind);

    /// <summary>
    /// The number of items the desk shows as expected actions, counted from the full list of items.
    /// </summary>
    public static async Task<int> ActionItemCountAsync(this ApiClient client) =>
        (await client.ItemsAsync()).Count(item => !item["isReference"]!.GetValue<bool>());
}
