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
        Track(created);
        return created;
    }

    /// <summary>
    /// Adds an administrator that became able to sign in some other way, such as by an invitation, to the candidates for the active one.
    /// </summary>
    /// <param name="account">The administrator's account.</param>
    public void Track(TestUser account)
    {
        lock (administrators)
        {
            administrators.Add((account.Email, account.Password));
        }
    }

    /// <summary>
    /// Signs in as the active administrator and checks that there is exactly one, which is the invariant every test leaves behind.
    /// An account that is blocked or demoted at the time of the call is dropped from the candidates for good,
    /// so a test that activates such an account again must block it once more before the next call.
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
/// The last active global administrator can be neither blocked, demoted, dismissed nor deleted, also when changes race.
/// An administrator who has been invited and has not set a password yet is not an active one.
/// </summary>
public sealed class LastAdministratorTests(AdministratorsHost fixture, TestEnvironment environment) : IClassFixture<AdministratorsHost>
{
    private const string LastAdministratorWording = "последн";
    private const string EmployeeWording = "сотрудник";

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
    public async Task Ac4_InvitedAdministrator_IsNotAnotherActiveOneUntilItSetsAPassword()
    {
        var (sole, soleId) = await fixture.ActiveAdministratorAsync();
        var invited = await sole.InviteUserAsync(Scenarios.GlobalAdmin);
        var token = (await environment.Mail.WaitForAsync(invited.Email))[0].Token;
        var current = await sole.GetUserAsync(soleId);

        var block = await sole.PostAsync($"/api/v1/users/{soleId}/block", ifMatch: current.ETag);
        var demote = await sole.PutAsync(
            $"/api/v1/users/{soleId}",
            new { email = current.Json!["email"]!.GetValue<string>(), role = Scenarios.User, employeeId = (Guid?)null },
            current.ETag);

        block.Status.Should().Be(HttpStatusCode.Conflict, "an administrator who cannot sign in is not another active one");
        block.Json!["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        demote.Status.Should().Be(HttpStatusCode.Conflict);
        demote.Json!["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        (await fixture.Host.Anonymous().PostAsync("/api/v1/auth/accept-invitation", new { token, password = invited.Password })).Expect(HttpStatusCode.NoContent);
        fixture.Track(invited);
        (await sole.PostAsync($"/api/v1/users/{soleId}/block", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
        (await fixture.ActiveAdministratorAsync()).Id.Should().Be(invited.Id, "once registered, the invited administrator is the active one");
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

    [Fact]
    public async Task Ac8_UnblockingAnAdministratorWhoseEmployeeWasDismissed_IsRefusedUntilTheEmployeeWorksAgain()
    {
        var (sole, soleId) = await fixture.ActiveAdministratorAsync();
        var unit = await sole.CreateUnitAsync();
        var employee = await sole.CreateEmployeeAsync(unit.Id);
        var bound = await fixture.NewAdministratorAsync(sole, employee.Id);
        var dismissed = (await sole.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag)).Expect(HttpStatusCode.OK);
        var blocked = await sole.GetUserAsync(bound.Id);

        var unblock = await sole.PostAsync($"/api/v1/users/{bound.Id}/unblock", ifMatch: blocked.ETag);
        var blockSole = await sole.PostAsync($"/api/v1/users/{soleId}/block", ifMatch: (await sole.GetUserAsync(soleId)).ETag);

        blocked.Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue("dismissing the employee blocked the account");
        unblock.Status.Should().Be(HttpStatusCode.Conflict, unblock.Body);
        unblock.Json!["detail"]!.GetValue<string>().Should().Contain(EmployeeWording);
        blockSole.Status.Should().Be(HttpStatusCode.Conflict, "an administrator who cannot sign in is not another active one");
        blockSole.Json!["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        var refused = await sole.GetUserAsync(bound.Id);
        refused.Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue();
        refused.ETag.Should().Be(blocked.ETag, "a refused unblock changes nothing");

        (await sole.PostAsync($"/api/v1/employees/{employee.Id}/rehire", ifMatch: dismissed.ETag)).Expect(HttpStatusCode.OK);
        var unblocked = (await sole.PostAsync($"/api/v1/users/{bound.Id}/unblock", ifMatch: blocked.ETag)).Expect(HttpStatusCode.OK);
        await bound.SignInAsync(fixture.Host);
        (await sole.PostAsync($"/api/v1/users/{bound.Id}/block", ifMatch: unblocked.ETag)).Expect(HttpStatusCode.OK);
        (await fixture.ActiveAdministratorAsync()).Id.Should().Be(soleId);
    }

    [Theory]
    [InlineData("block", "block")]
    [InlineData("dismiss", "dismiss")]
    [InlineData("dismiss", "block")]
    public async Task Ac7_TwoConcurrentRemovalsOfTheOnlyTwoAdministrators_LeaveExactlyOne(string removalByX, string removalByY)
    {
        var (current, currentId) = await fixture.ActiveAdministratorAsync();
        var unit = await current.CreateUnitAsync();
        var employeeX = await current.CreateEmployeeAsync(unit.Id);
        var employeeY = await current.CreateEmployeeAsync(unit.Id);
        var x = await fixture.NewAdministratorAsync(current, employeeX.Id);
        var y = await fixture.NewAdministratorAsync(current, employeeY.Id);
        await current.InviteUserAsync(Scenarios.GlobalAdmin);
        var clientX = await x.SignInAsync(fixture.Host);
        var clientY = await y.SignInAsync(fixture.Host);
        (await clientX.PostAsync($"/api/v1/users/{currentId}/block", ifMatch: (await clientX.GetUserAsync(currentId)).ETag)).Expect(HttpStatusCode.OK);
        var versionOfY = (await clientX.GetUserAsync(y.Id)).ETag!;
        var versionOfX = (await clientY.GetUserAsync(x.Id)).ETag!;

        await using var held = await RowLock.HoldActiveAdministratorsAsync(fixture.Host);
        var removeY = Remove(removalByX, clientX, y.Id, versionOfY, employeeY);
        await WaitForTheLockAsync(removeY, 1);
        var removeX = Remove(removalByY, clientY, x.Id, versionOfX, employeeX);
        await WaitForTheLockAsync(removeX, 2);
        await held.ReleaseAsync();
        var results = await Task.WhenAll(removeY, removeX);

        results[0].Status.Should().Be(HttpStatusCode.OK, results[0].Body);
        results[1].Status.Should().Be(HttpStatusCode.Conflict, "the removal that came second finds the first one done and no other active administrator, the invited one not counting: " + results[1].Body);
        results[1].Json!["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        (await fixture.ActiveAdministratorAsync()).Id.Should().Be(x.Id);
    }

    private static async Task<ApiResponse> Remove(string kind, ApiClient by, Guid victimId, string victimVersion, ApiResponse victimEmployee) =>
        kind == "block"
            ? await by.PostAsync($"/api/v1/users/{victimId}/block", ifMatch: victimVersion)
            : await by.PostAsync($"/api/v1/employees/{victimEmployee.Id}/dismiss", ifMatch: victimEmployee.ETag);

    private async Task WaitForTheLockAsync(Task<ApiResponse> request, int queued)
    {
        var waiting = RowLock.UntilWaitingAsync(fixture.Host, queued);
        await Task.WhenAny(waiting, request);
        request.IsCompleted.Should().BeFalse($"removal {queued} must wait for the administrators lock the test holds, not answer: {(request.IsCompleted ? (await request).Body : string.Empty)}");
        await waiting;
    }
}
