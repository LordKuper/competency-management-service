using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// AC-5: a change needs <c>If-Match</c> (428 without it, 400 when malformed) and a stale version is refused with 412 instead of overwriting;
/// each mutating endpoint is checked on its own because each applies the version itself.
/// </summary>
public sealed class OptimisticConcurrencyTests(TestEnvironment environment)
{
    private const string ProblemJson = "application/problem+json";
    private static readonly Guid UnknownId = Guid.Parse("00000000-0000-0000-0000-00000000dead");

    private static readonly (string Method, string Path, object? Body)[] VersionedEndpoints =
    [
        ("PUT", "/api/v1/users/{id}", new { email = "a@test.local", role = Scenarios.User, employeeId = (Guid?)null }),
        ("POST", "/api/v1/users/{id}/block", null),
        ("POST", "/api/v1/users/{id}/unblock", null),
        ("POST", "/api/v1/users/{id}/reset-password", new { newPassword = Scenarios.UserPassword }),
        ("PUT", "/api/v1/org-units/{id}", new { name = "Отдел", headEmployeeId = (Guid?)null }),
        ("POST", "/api/v1/org-units/{id}/move", new { parentId = (Guid?)null }),
        ("POST", "/api/v1/org-units/{id}/deactivate", null),
        ("POST", "/api/v1/org-units/{id}/activate", null),
        ("PUT", "/api/v1/employees/{id}", new { lastName = "Иванов", firstName = "Иван", middleName = (string?)null, position = "Инженер", orgUnitId = UnknownId }),
        ("POST", "/api/v1/employees/{id}/dismiss", null),
        ("POST", "/api/v1/employees/{id}/rehire", null),
        ("DELETE", "/api/v1/employees/{id}", null),
    ];

    [Fact]
    public async Task Ac5_EveryVersionedEndpoint_RequiresIfMatchWith428()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();

        var wrong = new List<string>();
        foreach (var (method, template, body) in VersionedEndpoints)
        {
            var response = await admin.SendAsync(new HttpMethod(method), template.Replace("{id}", UnknownId.ToString()), body);
            if (response.Status != HttpStatusCode.PreconditionRequired || response.ContentType != ProblemJson)
            {
                wrong.Add($"{method} {template}: {response.Code} {response.ContentType}");
            }
        }

        wrong.Should().BeEmpty("a write without If-Match must be refused with 428 problem+json before anything is read");
    }

    [Theory]
    [InlineData("W/\"1\"")]
    [InlineData("1")]
    [InlineData("\"abc\"")]
    [InlineData("*")]
    [InlineData("\"1\", \"2\"")]
    public async Task Ac5_MalformedIfMatch_IsRefusedWith400_AndChangesNothing(string ifMatch)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();

        var refused = await admin.PostAsync($"/api/v1/org-units/{unit.Id}/deactivate", ifMatch: ifMatch);

        refused.Status.Should().Be(HttpStatusCode.BadRequest, refused.Body);
        (await admin.GetUnitAsync(unit.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Ac5_SuccessfulWrite_ReturnsTheNewVersionAsETag()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();

        var renamed = (await admin.PutAsync($"/api/v1/org-units/{unit.Id}", new { name = Scenarios.Unique("Новое"), headEmployeeId = (Guid?)null }, unit.ETag))
            .Expect(HttpStatusCode.OK);

        renamed.ETag.Should().NotBe(unit.ETag);
        renamed.ETag.Should().Be((await admin.GetUnitAsync(unit.Id)).ETag);
        renamed.Json!["version"]!.GetValue<int>().Should().Be(int.Parse(renamed.ETag!.Trim('"')));
    }

    [Theory]
    [InlineData("update")]
    [InlineData("block")]
    [InlineData("reset-password")]
    public async Task Ac5_StaleUserWrite_IsRefusedWith412_AndLeavesTheAccountAsItWas(string action)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var user = await admin.CreateUserAsync();
        var bumped = (await admin.PutAsync(
            $"/api/v1/users/{user.Id}",
            new { email = $"{Scenarios.Unique("moved")}@test.local", role = Scenarios.User, employeeId = (Guid?)null },
            user.ETag)).Expect(HttpStatusCode.OK);

        var stale = action switch
        {
            "update" => await admin.PutAsync($"/api/v1/users/{user.Id}", new { email = user.Email, role = Scenarios.GlobalAdmin, employeeId = (Guid?)null }, user.ETag),
            "block" => await admin.PostAsync($"/api/v1/users/{user.Id}/block", ifMatch: user.ETag),
            _ => await admin.PostAsync($"/api/v1/users/{user.Id}/reset-password", new { newPassword = "Other-Pass-12345!" }, user.ETag),
        };

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        stale.ContentType.Should().Be(ProblemJson);
        var after = await admin.GetUserAsync(user.Id);
        after.ETag.Should().Be(bumped.ETag, "a refused write must not change the account");
        after.Json!["role"]!.GetValue<string>().Should().Be(Scenarios.User);
        after.Json["isBlocked"]!.GetValue<bool>().Should().BeFalse();
    }

    [Theory]
    [InlineData("update")]
    [InlineData("move")]
    [InlineData("deactivate")]
    public async Task Ac5_StaleUnitWrite_IsRefusedWith412_AndLeavesTheUnitAsItWas(string action)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var parent = await admin.CreateUnitAsync();
        var unit = await admin.CreateUnitAsync();
        var bumped = (await admin.PutAsync($"/api/v1/org-units/{unit.Id}", new { name = Scenarios.Unique("Сдвинут"), headEmployeeId = (Guid?)null }, unit.ETag))
            .Expect(HttpStatusCode.OK);

        var stale = action switch
        {
            "update" => await admin.PutAsync($"/api/v1/org-units/{unit.Id}", new { name = "Затёртое имя", headEmployeeId = (Guid?)null }, unit.ETag),
            "move" => await admin.PostAsync($"/api/v1/org-units/{unit.Id}/move", new { parentId = parent.Id }, unit.ETag),
            _ => await admin.PostAsync($"/api/v1/org-units/{unit.Id}/deactivate", ifMatch: unit.ETag),
        };

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        stale.ContentType.Should().Be(ProblemJson);
        var after = await admin.GetUnitAsync(unit.Id);
        after.ETag.Should().Be(bumped.ETag);
        after.Json!["parentId"].Should().BeNull();
        after.Json["isActive"]!.GetValue<bool>().Should().BeTrue();
    }

    [Theory]
    [InlineData("update")]
    [InlineData("dismiss")]
    [InlineData("delete")]
    public async Task Ac5_StaleEmployeeWrite_IsRefusedWith412_AndLeavesTheEmployeeAsItWas(string action)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id, firstName: "Пётр");
        var bumped = (await admin.PutAsync(
            $"/api/v1/employees/{employee.Id}",
            new { lastName = employee.Json!["lastName"]!.GetValue<string>(), firstName = "Павел", middleName = (string?)null, position = "Инженер", orgUnitId = unit.Id },
            employee.ETag)).Expect(HttpStatusCode.OK);

        var stale = action switch
        {
            "update" => await admin.PutAsync(
                $"/api/v1/employees/{employee.Id}",
                new { lastName = "Затёртый", firstName = "Пётр", middleName = (string?)null, position = "Инженер", orgUnitId = unit.Id },
                employee.ETag),
            "dismiss" => await admin.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag),
            _ => await admin.DeleteAsync($"/api/v1/employees/{employee.Id}", employee.ETag),
        };

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        stale.ContentType.Should().Be(ProblemJson);
        var after = await admin.GetEmployeeAsync(employee.Id);
        after.ETag.Should().Be(bumped.ETag);
        after.Json!["firstName"]!.GetValue<string>().Should().Be("Павел");
        after.Json["isActive"]!.GetValue<bool>().Should().BeTrue();
    }
}
