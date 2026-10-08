using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// An account created through the API, with the credentials needed to sign in as it.
/// </summary>
/// <param name="Id">The account id.</param>
/// <param name="Email">The e-mail, which is also the sign-in name.</param>
/// <param name="Password">The password the account has once it is registered, which is the one tests sign in with.</param>
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

    private static readonly Lazy<string> UserPasswordHash = new(() => new PasswordHasher<object>().HashPassword(new object(), UserPassword));

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

    /// <summary>
    /// Creates an account the way an administrator does: without a password, invited by e-mail, so it cannot sign in yet.
    /// </summary>
    public static async Task<TestUser> InviteUserAsync(this ApiClient admin, string role = User, Guid? employeeId = null, string? email = null)
    {
        var address = email ?? $"{Unique("user")}@test.local";
        var created = (await admin.PostAsync("/api/v1/users", new { email = address, role, employeeId })).Expect(HttpStatusCode.Created);
        return new TestUser(created.Id, address, UserPassword, created.ETag!);
    }

    /// <summary>
    /// Creates an account that has already set its password, so a test that is not about registration can sign in as it.
    /// The registration is written to the database directly, as the invitation link would have left it: the password set and the link spent.
    /// </summary>
    public static async Task<TestUser> CreateUserAsync(this ApiClient admin, string role = User, Guid? employeeId = null, string? email = null)
    {
        var invited = await admin.InviteUserAsync(role, employeeId, email);
        await admin.Host.ExecuteAsync(
            $"UPDATE users SET password_hash = '{UserPasswordHash.Value}', link_token_hash = NULL, link_expires_at = NULL, link_issued_at = NULL, link_security_stamp = NULL WHERE id = '{invited.Id}'");
        return invited;
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

    /// <summary>
    /// The text as it appears inside a JSON string, where the log writer escapes what is not plain ASCII, so a search for the text finds it there.
    /// </summary>
    public static string JsonEscaped(this string text) => JsonSerializer.Serialize(text)[1..^1];

    public static string[] Actions(this JsonArray rows) => [.. rows.Select(row => row!["action"]!.GetValue<string>()).Order()];
}
