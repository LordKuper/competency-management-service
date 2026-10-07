using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// AC-6 and AC-12: every protected endpoint answers 401 without a session and 403 to a signed-in user without the administrator role,
/// whichever resource the request names, and the user role reads only the restricted projection of active data.
/// </summary>
public sealed class AuthorizationTests(TestEnvironment environment)
{
    private const string ProblemJson = "application/problem+json";

    private static readonly Guid UnknownId = Guid.Parse("00000000-0000-0000-0000-00000000dead");

    private static readonly (string Method, string Path)[] AdministratorEndpoints =
    [
        ("GET", "/api/v1/users"),
        ("GET", "/api/v1/users/{id}"),
        ("POST", "/api/v1/users"),
        ("PUT", "/api/v1/users/{id}"),
        ("POST", "/api/v1/users/{id}/block"),
        ("POST", "/api/v1/users/{id}/unblock"),
        ("POST", "/api/v1/users/{id}/reset-password"),
        ("GET", "/api/v1/audit"),
        ("POST", "/api/v1/org-units"),
        ("PUT", "/api/v1/org-units/{id}"),
        ("POST", "/api/v1/org-units/{id}/move"),
        ("POST", "/api/v1/org-units/{id}/deactivate"),
        ("POST", "/api/v1/org-units/{id}/activate"),
        ("POST", "/api/v1/employees"),
        ("PUT", "/api/v1/employees/{id}"),
        ("GET", "/api/v1/employees/{id}/impact"),
        ("POST", "/api/v1/employees/{id}/dismiss"),
        ("POST", "/api/v1/employees/{id}/rehire"),
        ("DELETE", "/api/v1/employees/{id}"),
    ];

    private static readonly (string Method, string Path)[] SignedInEndpoints =
    [
        ("GET", "/api/v1/org-units"),
        ("GET", "/api/v1/org-units/tree"),
        ("GET", "/api/v1/org-units/{id}"),
        ("GET", "/api/v1/org-units/{id}/subtree"),
        ("GET", "/api/v1/org-units/{id}/path"),
        ("GET", "/api/v1/org-units/{id}/summary"),
        ("GET", "/api/v1/employees"),
        ("GET", "/api/v1/employees/{id}"),
        ("GET", "/api/v1/auth/me"),
        ("POST", "/api/v1/auth/logout"),
        ("POST", "/api/v1/auth/change-password"),
    ];

    [Fact]
    public async Task Ac6_AdministratorEndpoints_RefuseAnonymousWith401AndUserWith403_EvenForAnUnknownId()
    {
        var host = await environment.SharedHostAsync();
        var user = await (await (await host.AdminAsync()).CreateUserAsync()).SignInAsync(host);
        var anonymous = host.Anonymous();

        var wrong = new List<string>();
        foreach (var (method, template) in AdministratorEndpoints)
        {
            var path = template.Replace("{id}", UnknownId.ToString());
            var asAnonymous = await anonymous.SendAsync(new HttpMethod(method), path);
            var asUser = await user.SendAsync(new HttpMethod(method), path);
            if (asAnonymous.Status != HttpStatusCode.Unauthorized || asUser.Status != HttpStatusCode.Forbidden
                || asAnonymous.ContentType != ProblemJson || asUser.ContentType != ProblemJson)
            {
                wrong.Add($"{method} {template}: anonymous {asAnonymous.Code} {asAnonymous.ContentType}, user {asUser.Code} {asUser.ContentType}");
            }
        }

        wrong.Should().BeEmpty("every administrator endpoint must answer 401 (anonymous) and 403 (user) as problem+json");
    }

    [Fact]
    public async Task Ac6_SignedInEndpoints_RefuseAnonymousWith401()
    {
        var host = await environment.SharedHostAsync();
        var anonymous = host.Anonymous();

        var wrong = new List<string>();
        foreach (var (method, template) in SignedInEndpoints)
        {
            var response = await anonymous.SendAsync(new HttpMethod(method), template.Replace("{id}", UnknownId.ToString()));
            if (response.Status != HttpStatusCode.Unauthorized)
            {
                wrong.Add($"{method} {template}: {response.Code}");
            }
        }

        wrong.Should().BeEmpty("every non-public endpoint must answer 401 without a session");
    }

    [Fact]
    public async Task Ac12_UserRole_SeesOnlyActiveUnitsAndWorkingEmployees_WithoutStatusOrVersion()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var active = await admin.CreateUnitAsync();
        var inactive = await admin.CreateUnitAsync();
        (await admin.PostAsync($"/api/v1/org-units/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);
        var working = await admin.CreateEmployeeAsync(active.Id);
        var dismissed = await admin.CreateEmployeeAsync(active.Id);
        (await admin.PostAsync($"/api/v1/employees/{dismissed.Id}/dismiss", ifMatch: dismissed.ETag)).Expect(HttpStatusCode.OK);
        var account = await admin.CreateUserAsync(employeeId: working.Id);
        var user = await (await admin.CreateUserAsync()).SignInAsync(host);

        (await admin.GetUnitAsync(inactive.Id)).Status.Should().Be(HttpStatusCode.OK);
        foreach (var hidden in new[] { "", "/subtree", "/path", "/summary" })
        {
            (await user.GetAsync($"/api/v1/org-units/{inactive.Id}{hidden}")).Status.Should().Be(HttpStatusCode.NotFound, hidden);
        }

        var units = (await user.GetAsync("/api/v1/org-units?pageSize=200")).Expect(HttpStatusCode.OK);
        var tree = (await user.GetAsync("/api/v1/org-units/tree")).Expect(HttpStatusCode.OK);
        Ids(units.Json!["items"]!).Should().Contain(active.Id).And.NotContain(inactive.Id);
        Ids(tree.Json!).Should().Contain(active.Id).And.NotContain(inactive.Id);

        (await user.GetAsync($"/api/v1/employees/{dismissed.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
        var seenByUser = (await user.GetAsync($"/api/v1/employees/{working.Id}")).Expect(HttpStatusCode.OK);
        var seenByAdmin = (await admin.GetAsync($"/api/v1/employees/{working.Id}")).Expect(HttpStatusCode.OK);
        seenByUser.Json!["email"]!.GetValue<string>().Should().Be(account.Email);
        seenByUser.Json.AsObject().ContainsKey("isActive").Should().BeFalse();
        seenByUser.Json.AsObject().ContainsKey("version").Should().BeFalse();
        seenByUser.ETag.Should().BeNull();
        seenByAdmin.Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        seenByAdmin.ETag.Should().NotBeNull();

        var list = (await user.GetAsync($"/api/v1/employees?orgUnitId={active.Id}&isActive=false")).Expect(HttpStatusCode.OK);
        list.Json!["total"]!.GetValue<int>().Should().Be(0, "a user never sees dismissed employees, whatever the filter says");
    }

    private static IEnumerable<Guid> Ids(System.Text.Json.Nodes.JsonNode rows) =>
        rows.AsArray().Select(row => Guid.Parse(row!["id"]!.GetValue<string>()));
}
