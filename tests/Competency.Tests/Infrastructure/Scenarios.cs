using System.Net;
using System.Text.Json.Nodes;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// An account created through the API, with the credentials needed to sign in as it.
/// </summary>
/// <param name="Id">The account id.</param>
/// <param name="Email">The e-mail, which is also the sign-in name.</param>
/// <param name="Password">The password it was created with.</param>
/// <param name="ETag">The entity tag at creation.</param>
public sealed record TestUser(Guid Id, string Email, string Password, string ETag);

/// <summary>
/// Builders for the data tests arrange through the API as an administrator; every call uses a unique name so tests sharing a host never collide.
/// </summary>
public static class Scenarios
{
    public const string UserPassword = "Valid-Pass-12345!";
    public const string User = "User";
    public const string GlobalAdmin = "GlobalAdmin";

    public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    public static async Task<ApiResponse> CreateUnitAsync(this ApiClient admin, Guid? parentId = null, string? name = null) =>
        (await admin.PostAsync("/api/v1/org-units", new { name = name ?? Unique("Отдел"), parentId })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> CreateEmployeeAsync(
        this ApiClient admin,
        Guid unitId,
        string? lastName = null,
        string firstName = "Иван",
        string? middleName = null,
        string position = "Инженер") =>
        (await admin.PostAsync(
            "/api/v1/employees",
            new { lastName = lastName ?? Unique("Иванов"), firstName, middleName, position, orgUnitId = unitId }))
        .Expect(HttpStatusCode.Created);

    public static async Task<TestUser> CreateUserAsync(this ApiClient admin, string role = User, Guid? employeeId = null, string? email = null)
    {
        var address = email ?? $"{Unique("user")}@test.local";
        var created = (await admin.PostAsync("/api/v1/users", new { email = address, password = UserPassword, role, employeeId }))
            .Expect(HttpStatusCode.Created);
        return new TestUser(created.Id, address, UserPassword, created.ETag!);
    }

    public static Task<ApiClient> SignInAsync(this TestUser user, ApiHost host) => host.LoginAsync(user.Email, user.Password);

    public static async Task<ApiResponse> GetUserAsync(this ApiClient admin, Guid id) =>
        (await admin.GetAsync($"/api/v1/users/{id}")).Expect(HttpStatusCode.OK);

    public static async Task<ApiResponse> GetUnitAsync(this ApiClient client, Guid id) =>
        (await client.GetAsync($"/api/v1/org-units/{id}")).Expect(HttpStatusCode.OK);

    public static async Task<ApiResponse> GetEmployeeAsync(this ApiClient client, Guid id) =>
        (await client.GetAsync($"/api/v1/employees/{id}")).Expect(HttpStatusCode.OK);

    /// <summary>
    /// Sets the head of a unit with the unit's current version, so a rejected head shows up as the status of the returned response.
    /// </summary>
    public static async Task<ApiResponse> SetHeadAsync(this ApiClient admin, ApiResponse unit, Guid? headEmployeeId)
    {
        var current = await admin.GetUnitAsync(unit.Id);
        return await admin.PutAsync(
            $"/api/v1/org-units/{unit.Id}",
            new { name = current.Json!["name"]!.GetValue<string>(), headEmployeeId },
            current.ETag);
    }

    /// <summary>
    /// Every journal row written by one request.
    /// </summary>
    public static async Task<JsonArray> AuditOfRequestAsync(this ApiClient admin, string requestId)
    {
        var page = (await admin.GetAsync($"/api/v1/audit?requestId={requestId}&pageSize=200")).Expect(HttpStatusCode.OK);
        return page.Json!["items"]!.AsArray();
    }

    public static string[] Actions(this JsonArray rows) => [.. rows.Select(row => row!["action"]!.GetValue<string>()).Order()];
}
