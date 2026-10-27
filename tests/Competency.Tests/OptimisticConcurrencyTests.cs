using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A change needs <c>If-Match</c> (428 without it, 400 when malformed) and a stale version is refused with 412 instead of overwriting;
/// each mutating endpoint is checked on its own because each applies the version itself.
/// </summary>
public sealed class OptimisticConcurrencyTests(TestEnvironment environment)
{
    private const string ProblemJson = "application/problem+json";
    private static readonly Guid UnknownId = Guid.Parse("00000000-0000-0000-0000-00000000dead");

    private static readonly (string Method, string Path, object? Body)[] VersionedEndpoints =
    [
        ("PUT", "/api/v1/users/{id}", new { email = "a@test.local", roles = Scenarios.NoRoles, employeeId = (Guid?)null }),
        ("POST", "/api/v1/users/{id}/block", null),
        ("POST", "/api/v1/users/{id}/unblock", null),
        ("POST", "/api/v1/users/{id}/resend-invitation", null),
        ("POST", "/api/v1/users/{id}/send-password-reset", null),
        ("PUT", "/api/v1/org-units/{id}", new { name = "Отдел", headEmployeeId = (Guid?)null }),
        ("POST", "/api/v1/org-units/{id}/move", new { parentId = (Guid?)null }),
        ("POST", "/api/v1/org-units/{id}/deactivate", null),
        ("POST", "/api/v1/org-units/{id}/activate", null),
        ("PUT", "/api/v1/employees/{id}", Scenarios.EmployeeBody("Иванов", "Иван", UnknownId, UnknownId)),
        ("POST", "/api/v1/employees/{id}/dismiss", null),
        ("POST", "/api/v1/employees/{id}/rehire", null),
        ("DELETE", "/api/v1/employees/{id}", null),
        ("POST", "/api/v1/employees/{id}/move", new { fromOrgUnitId = UnknownId, positionId = UnknownId, toOrgUnitId = UnknownId }),
        ("DELETE", "/api/v1/org-units/{id}", null),
    ];

    private static readonly (string Method, string Path, object? Body)[] CatalogueEndpoints =
    [
        .. new[] { "directions", "tracks", "levels", "scales", "competencies", "blocks" }.SelectMany(kind => new (string, string, object?)[]
        {
            ("POST", $"/api/v1/career/{kind}/{{id}}/approve", null),
            ("POST", $"/api/v1/career/{kind}/{{id}}/deactivate", null),
            ("POST", $"/api/v1/career/{kind}/{{id}}/activate", null),
            ("DELETE", $"/api/v1/career/{kind}/{{id}}", null),
        }),
        ("PUT", "/api/v1/career/directions/{id}", new { name = "Направление" }),
        ("PUT", "/api/v1/career/tracks/{id}", new { name = "Трек" }),
        ("PUT", "/api/v1/career/levels/{id}", new { name = "Уровень" }),
        ("PUT", "/api/v1/career/scales/{id}", new { name = "Шкала", points = CareerScenarios.Points() }),
        ("PUT", "/api/v1/career/competencies/{id}", new { name = "Навык", description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)null }),
        ("PUT", "/api/v1/career/blocks/{id}", new { name = "Блок", scopeUnitId = (Guid?)null, competencyIds = Array.Empty<Guid>() }),
        ("POST", "/api/v1/career/levels/{id}/positions", new { positionId = UnknownId }),
        ("DELETE", "/api/v1/career/levels/{id}/positions/{id}", null),
        ("POST", "/api/v1/career/scales/{id}/make-default", null),
        ("PUT", "/api/v1/career/tracks/{id}/level-order", new { levelIds = Array.Empty<Guid>() }),
        ("DELETE", "/api/v1/career/matrices/{id}", null),
        ("PUT", "/api/v1/career/matrix-versions/{id}/content", new { levels = Array.Empty<object>(), competencies = Array.Empty<object>(), blockWeights = Array.Empty<object>(), requirements = Array.Empty<object>() }),
        ("POST", "/api/v1/career/matrix-versions/{id}/submit", null),
        ("POST", "/api/v1/career/matrix-versions/{id}/approve", null),
        ("POST", "/api/v1/career/matrix-versions/{id}/return", new { comment = (string?)null }),
        ("POST", "/api/v1/career/matrix-versions/{id}/accept-changes", null),
        ("POST", "/api/v1/career/matrix-versions/{id}/accept-and-revise", null),
        ("POST", "/api/v1/career/matrix-versions/{id}/rollback-changes", null),
        ("PUT", "/api/v1/positions/{id}", new { name = "Должность" }),
        ("POST", "/api/v1/positions/{id}/deactivate", null),
        ("POST", "/api/v1/positions/{id}/activate", null),
    ];

    [Fact]
    public async Task Ac5_EveryVersionedEndpoint_RequiresIfMatchWith428()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();

        var wrong = new List<string>();
        foreach (var (method, template, body) in VersionedEndpoints.Concat(CatalogueEndpoints))
        {
            var response = await admin.SendAsync(new HttpMethod(method), template.Replace("{id}", UnknownId.ToString()), body);
            if (response.Status != HttpStatusCode.PreconditionRequired || response.ContentType != ProblemJson)
            {
                wrong.Add($"{method} {template}: {response.Code} {response.ContentType}");
            }
        }

        wrong.Should().BeEmpty("a write without If-Match must be refused with 428 problem+json before anything is read");
    }

    [Fact]
    public void Ac5_EveryOperationOfTheContractThatTakesIfMatch_IsCheckedHere_AndNoCheckedRowIsOfARouteThatIsGone()
    {
        var versioned = ApiContract.Operations()
            .Where(operation => operation.Operation["parameters"]?.AsArray().Any(parameter => parameter!["name"]!.GetValue<string>() == "If-Match") == true)
            .Select(operation => (operation.Method, operation.Path))
            .ToHashSet();
        var listed = VersionedEndpoints.Concat(CatalogueEndpoints).Select(row => (row.Method, Path: ApiContract.Normalize(row.Path))).ToHashSet();

        versioned.Except(listed).Should().BeEmpty("an endpoint that takes If-Match must be listed above, so that its 428 is tested");
        listed.Except(versioned).Should().BeEmpty("a listed endpoint that takes no If-Match is a stale row");
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
    [InlineData("send-password-reset")]
    [InlineData("resend-invitation")]
    public async Task Ac5_StaleUserWrite_IsRefusedWith412_AndLeavesTheAccountAsItWas(string action)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var user = action == "resend-invitation" ? await admin.InviteUserAsync() : await admin.CreateUserAsync();
        var bumped = (await admin.PutAsync(
            $"/api/v1/users/{user.Id}",
            new { email = $"{Scenarios.Unique("moved")}@test.local", roles = Scenarios.NoRoles, employeeId = (Guid?)null },
            user.ETag)).Expect(HttpStatusCode.OK);

        var stale = action switch
        {
            "update" => await admin.PutAsync($"/api/v1/users/{user.Id}", new { email = user.Email, roles = Scenarios.AdminRoles, employeeId = (Guid?)null }, user.ETag),
            "block" => await admin.PostAsync($"/api/v1/users/{user.Id}/block", ifMatch: user.ETag),
            _ => await admin.PostAsync($"/api/v1/users/{user.Id}/{action}", ifMatch: user.ETag),
        };

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        stale.ContentType.Should().Be(ProblemJson);
        var after = await admin.GetUserAsync(user.Id);
        after.ETag.Should().Be(bumped.ETag, "a refused write must not change the account");
        after.Json!["roles"]!.AsArray().Should().BeEmpty();
        after.Json["isBlocked"]!.GetValue<bool>().Should().BeFalse();
    }

    [Theory]
    [InlineData("update")]
    [InlineData("move")]
    [InlineData("deactivate")]
    [InlineData("delete")]
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
            "delete" => await admin.DeleteAsync($"/api/v1/org-units/{unit.Id}", unit.ETag),
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
    [InlineData("move")]
    [InlineData("dismiss")]
    [InlineData("delete")]
    public async Task Ac5_StaleEmployeeWrite_IsRefusedWith412_AndLeavesTheEmployeeAsItWas(string action)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id, firstName: "Пётр");
        var bumped = (await admin.PutAsync(
            $"/api/v1/employees/{employee.Id}",
            Scenarios.EmployeeBody(employee.Json!["lastName"]!.GetValue<string>(), "Павел", employee.Json["positionId"]!.GetValue<Guid>(), unit.Id),
            employee.ETag)).Expect(HttpStatusCode.OK);

        var stale = action switch
        {
            "update" => await admin.PutAsync(
                $"/api/v1/employees/{employee.Id}",
                Scenarios.EmployeeBody("Затёртый", "Пётр", employee.Json["positionId"]!.GetValue<Guid>(), unit.Id),
                employee.ETag),
            "move" => await admin.PostAsync(
                $"/api/v1/employees/{employee.Id}/move",
                new { fromOrgUnitId = unit.Id, positionId = employee.Json["positionId"]!.GetValue<Guid>(), toOrgUnitId = (await admin.CreateUnitAsync()).Id },
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
        after.Json["orgUnitId"]!.GetValue<string>().Should().Be(unit.Id.ToString());
    }
}
