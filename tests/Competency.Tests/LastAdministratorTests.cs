using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A host that tracks the administrators created in its database, so that a test can sign in as the one that is still active.
/// Every test of the class must end with exactly one active administrator.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class AdministratorsHost(TestEnvironment environment) : HostFixture(environment, null)
{
    private readonly List<(string Email, string Password)> administrators = [(ApiHost.AdminEmail, ApiHost.AdminPassword)];

    public async Task<TestUser> NewAdministratorAsync(ApiClient by, Guid? employeeId = null)
    {
        var created = await by.CreateUserAsync(Scenarios.GlobalAdmin, employeeId);
        lock (administrators)
        {
            administrators.Add((created.Email, created.Password));
        }

        return created;
    }

    /// <summary>
    /// Signs in as the active administrator and checks that there is exactly one, which is the invariant every test leaves behind.
    /// An account that is blocked or demoted never becomes active again in these tests, so it is dropped from the candidates for good.
    /// </summary>
    /// <returns>The session and the account id of the active administrator.</returns>
    public async Task<(ApiClient Client, Guid Id)> ActiveAdministratorAsync()
    {
        (string Email, string Password)[] candidates;
        lock (administrators)
        {
            candidates = [.. administrators];
        }

        var active = new List<(ApiClient Client, Guid Id, string Email, string Password)>();
        foreach (var (email, password) in candidates)
        {
            var client = Host.Anonymous();
            var login = await client.PostAsync("/api/v1/auth/login", new { email, password });
            if (login.Status == HttpStatusCode.OK && login.Json!["role"]!.GetValue<string>() == Scenarios.GlobalAdmin)
            {
                active.Add((client, Guid.Parse(login.Json["id"]!.GetValue<string>()), email, password));
            }
        }

        lock (administrators)
        {
            administrators.RemoveAll(candidate => !active.Any(account => account.Email == candidate.Email));
        }

        active.Should().ContainSingle("the last administrator must never be removed and no test may leave two");
        return (active[0].Client, active[0].Id);
    }
}

/// <summary>
/// AC-7 and AC-18: the last active global administrator can be neither blocked, demoted, dismissed nor deleted, also when changes race.
/// </summary>
public sealed class LastAdministratorTests(AdministratorsHost fixture) : IClassFixture<AdministratorsHost>
{
    private const string LastAdministratorWording = "последн";

    [Fact]
    public async Task Ac7_BlockingTheLastActiveAdministrator_IsRefused()
    {
        var (sole, soleId) = await fixture.ActiveAdministratorAsync();
        var current = await sole.GetUserAsync(soleId);

        var refused = await sole.PostAsync($"/api/v1/users/{soleId}/block", ifMatch: current.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        (await sole.GetUserAsync(soleId)).Json!["isBlocked"]!.GetValue<bool>().Should().BeFalse();
        (await sole.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.OK, "the refused block must not end the session");
    }

    [Fact]
    public async Task Ac7_DemotingTheLastActiveAdministrator_IsRefused()
    {
        var (sole, soleId) = await fixture.ActiveAdministratorAsync();
        var current = await sole.GetUserAsync(soleId);

        var refused = await sole.PutAsync(
            $"/api/v1/users/{soleId}",
            new { email = current.Json!["email"]!.GetValue<string>(), role = Scenarios.User, employeeId = (Guid?)null },
            current.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        (await sole.GetUserAsync(soleId)).Json!["role"]!.GetValue<string>().Should().Be(Scenarios.GlobalAdmin);
    }

    [Fact]
    public async Task Ac7_BlockedOrDemotedAdministrators_DoNotCountAsAnotherActiveOne()
    {
        var (sole, soleId) = await fixture.ActiveAdministratorAsync();
        var blocked = await fixture.NewAdministratorAsync(sole);
        var demoted = await fixture.NewAdministratorAsync(sole);

        (await sole.PostAsync($"/api/v1/users/{blocked.Id}/block", ifMatch: blocked.ETag)).Expect(HttpStatusCode.OK);
        (await sole.PutAsync(
            $"/api/v1/users/{demoted.Id}",
            new { email = demoted.Email, role = Scenarios.User, employeeId = (Guid?)null },
            demoted.ETag)).Expect(HttpStatusCode.OK);

        var current = await sole.GetUserAsync(soleId);
        (await sole.PostAsync($"/api/v1/users/{soleId}/block", ifMatch: current.ETag)).Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Ac18_DismissingOrDeletingTheEmployeeOfTheLastAdministrator_IsRefusedAndChangesNothing()
    {
        var (sole, soleId) = await fixture.ActiveAdministratorAsync();
        var unit = await sole.CreateUnitAsync();
        var employee = await sole.CreateEmployeeAsync(unit.Id);
        (await sole.SetHeadAsync(unit, employee.Id)).Expect(HttpStatusCode.OK);
        var account = await sole.GetUserAsync(soleId);
        (await sole.PutAsync(
            $"/api/v1/users/{soleId}",
            new { email = account.Json!["email"]!.GetValue<string>(), role = Scenarios.GlobalAdmin, employeeId = employee.Id },
            account.ETag)).Expect(HttpStatusCode.OK);
        (sole, _) = await fixture.ActiveAdministratorAsync();
        var current = await sole.GetEmployeeAsync(employee.Id);

        var impact = (await sole.GetAsync($"/api/v1/employees/{employee.Id}/impact")).Expect(HttpStatusCode.OK);
        var dismissal = await sole.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: current.ETag);
        var deletion = await sole.DeleteAsync($"/api/v1/employees/{employee.Id}", current.ETag);

        impact.Json!["account"]!["isLastActiveAdministrator"]!.GetValue<bool>().Should().BeTrue();
        dismissal.Status.Should().Be(HttpStatusCode.Conflict);
        deletion.Status.Should().Be(HttpStatusCode.Conflict);
        dismissal.Json!["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        var after = await sole.GetEmployeeAsync(employee.Id);
        after.Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        (await sole.GetUnitAsync(unit.Id)).Json!["headEmployeeId"]!.GetValue<string>().Should().Be(employee.Id.ToString(), "a refused departure must keep the headship");
        (await sole.GetUserAsync(soleId)).Json!["isBlocked"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task Ac18_WhenAnotherAdministratorIsActive_TheEmployeeOfAnAdministratorCanBeDismissed()
    {
        var (sole, _) = await fixture.ActiveAdministratorAsync();
        var unit = await sole.CreateUnitAsync();
        var employee = await sole.CreateEmployeeAsync(unit.Id);
        var other = await fixture.NewAdministratorAsync(sole, employee.Id);

        var impact = (await sole.GetAsync($"/api/v1/employees/{employee.Id}/impact")).Expect(HttpStatusCode.OK);
        var dismissed = await sole.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag);

        impact.Json!["account"]!["isLastActiveAdministrator"]!.GetValue<bool>().Should().BeFalse();
        dismissed.Status.Should().Be(HttpStatusCode.OK, dismissed.Body);
        (await sole.GetUserAsync(other.Id)).Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue();
    }

    [Theory]
    [InlineData("block", "block")]
    [InlineData("dismiss", "dismiss")]
    [InlineData("dismiss", "block")]
    public async Task Ac7_TwoConcurrentRemovalsOfTheOnlyTwoAdministrators_LeaveExactlyOne(string removalByX, string removalByY)
    {
        const int Rounds = 4;
        for (var round = 0; round < Rounds; round++)
        {
            var (current, currentId) = await fixture.ActiveAdministratorAsync();
            var unit = await current.CreateUnitAsync();
            var employeeX = await current.CreateEmployeeAsync(unit.Id);
            var employeeY = await current.CreateEmployeeAsync(unit.Id);
            var x = await fixture.NewAdministratorAsync(current, employeeX.Id);
            var y = await fixture.NewAdministratorAsync(current, employeeY.Id);
            var clientX = await x.SignInAsync(fixture.Host);
            var clientY = await y.SignInAsync(fixture.Host);
            (await clientX.PostAsync($"/api/v1/users/{currentId}/block", ifMatch: (await clientX.GetUserAsync(currentId)).ETag)).Expect(HttpStatusCode.OK);
            var versionOfY = (await clientX.GetUserAsync(y.Id)).ETag!;
            var versionOfX = (await clientY.GetUserAsync(x.Id)).ETag!;
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var removeY = Remove(removalByX, clientX, y.Id, versionOfY, employeeY, gate.Task);
            var removeX = Remove(removalByY, clientY, x.Id, versionOfX, employeeX, gate.Task);
            gate.SetResult();
            var results = await Task.WhenAll(removeY, removeX);

            results.Count(result => result.Status == HttpStatusCode.OK).Should().Be(1, string.Join(" | ", results.Select(result => $"{result.Code} {result.Body}")));
            results.Where(result => result.Status != HttpStatusCode.OK).Should().OnlyContain(result => result.Status == HttpStatusCode.Conflict || result.Status == HttpStatusCode.Unauthorized);
            await fixture.ActiveAdministratorAsync();
        }
    }

    private static async Task<ApiResponse> Remove(string kind, ApiClient by, Guid victimId, string victimVersion, ApiResponse victimEmployee, Task start)
    {
        await start;
        return kind == "block"
            ? await by.PostAsync($"/api/v1/users/{victimId}/block", ifMatch: victimVersion)
            : await by.PostAsync($"/api/v1/employees/{victimEmployee.Id}/dismiss", ifMatch: victimEmployee.ETag);
    }
}
