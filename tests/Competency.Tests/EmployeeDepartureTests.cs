using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Dismissing or deleting an employee applies, in one transaction, exactly the changes its preview announced:
/// the headed unit loses its head, the bound account is blocked (and unbound on deletion), its live session ends, and every change is journaled.
/// </summary>
public sealed class EmployeeDepartureTests(TestEnvironment environment)
{
    [Fact]
    public async Task Ac18_Dismiss_AppliesExactlyThePreviewedChanges()
    {
        var (host, admin, scene) = await ArrangeAsync();

        var impact = (await admin.GetAsync($"/api/v1/employees/{scene.Employee.Id}/impact")).Expect(HttpStatusCode.OK);
        var dismissed = (await admin.PostAsync($"/api/v1/employees/{scene.Employee.Id}/dismiss", ifMatch: scene.Employee.ETag)).Expect(HttpStatusCode.OK);

        impact.Json!["headOfUnits"]!.AsArray().Select(unit => unit!["id"]!.GetValue<string>()).Should().Equal(scene.Unit.Id.ToString());
        impact.Json["account"]!["email"]!.GetValue<string>().Should().Be(scene.Account.Email);
        impact.Json["account"]!["isBlocked"]!.GetValue<bool>().Should().BeFalse();
        impact.Json["account"]!["isLastActiveAdministrator"]!.GetValue<bool>().Should().BeFalse();

        dismissed.Json!["isActive"]!.GetValue<bool>().Should().BeFalse();
        (await admin.GetUnitAsync(scene.Unit.Id)).Json!["headEmployeeId"].Should().BeNull();
        var account = await admin.GetUserAsync(scene.Account.Id);
        account.Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue();
        account.Json["employeeId"]!.GetValue<string>().Should().Be(scene.Employee.Id.ToString(), "dismissal keeps the binding");
        (await scene.AccountSession.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized, "the live session ends with the block");
        await AssertSignInRefusedAsync(host, scene.Account);

        var journal = await admin.AuditOfRequestAsync(dismissed.RequestId!);
        journal.Actions().Should().Equal("AppUser.Updated", "Employee.Updated", "OrgUnit.Updated");
    }

    [Fact]
    public async Task Ac18_Delete_RemovesTheEmployeeAndBlocksAndUnbindsTheAccount()
    {
        var (host, admin, scene) = await ArrangeAsync();

        var impact = (await admin.GetAsync($"/api/v1/employees/{scene.Employee.Id}/impact")).Expect(HttpStatusCode.OK);
        var deleted = (await admin.DeleteAsync($"/api/v1/employees/{scene.Employee.Id}", scene.Employee.ETag)).Expect(HttpStatusCode.NoContent);

        impact.Json!["headOfUnits"]!.AsArray().Should().HaveCount(1);
        (await admin.GetAsync($"/api/v1/employees/{scene.Employee.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
        (await admin.GetUnitAsync(scene.Unit.Id)).Json!["headEmployeeId"].Should().BeNull();
        var account = await admin.GetUserAsync(scene.Account.Id);
        account.Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue();
        account.Json["employeeId"].Should().BeNull("deletion detaches the account so that it can be unblocked");
        (await scene.AccountSession.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized);
        await AssertSignInRefusedAsync(host, scene.Account);

        var journal = await admin.AuditOfRequestAsync(deleted.RequestId!);
        journal.Actions().Should().Equal("AppUser.Updated", "Employee.Deleted", "OrgUnit.Updated");
        var accountChange = journal.Single(row => row!["action"]!.GetValue<string>() == "AppUser.Updated")!;
        accountChange["newValue"]!["employeeId"].Should().BeNull();
        accountChange["oldValue"]!["employeeId"]!.GetValue<string>().Should().Be(scene.Employee.Id.ToString());

        (await admin.PostAsync($"/api/v1/users/{scene.Account.Id}/unblock", ifMatch: account.ETag)).Expect(HttpStatusCode.OK);
        (await host.LoginAsync(scene.Account.Email, scene.Account.Password)).Should().NotBeNull("a detached account can be unblocked and signs in without an employee");
    }

    [Fact]
    public async Task Ac18_Rehire_NeitherUnblocksTheAccountNorRestoresTheHeadship()
    {
        var (host, admin, scene) = await ArrangeAsync();
        var dismissed = (await admin.PostAsync($"/api/v1/employees/{scene.Employee.Id}/dismiss", ifMatch: scene.Employee.ETag)).Expect(HttpStatusCode.OK);

        var again = await admin.PostAsync($"/api/v1/employees/{scene.Employee.Id}/dismiss", ifMatch: dismissed.ETag);
        var rehired = (await admin.PostAsync($"/api/v1/employees/{scene.Employee.Id}/rehire", ifMatch: dismissed.ETag)).Expect(HttpStatusCode.OK);
        var rehiredTwice = await admin.PostAsync($"/api/v1/employees/{scene.Employee.Id}/rehire", ifMatch: rehired.ETag);

        again.Status.Should().Be(HttpStatusCode.Conflict, "dismissing a dismissed employee is refused");
        rehiredTwice.Status.Should().Be(HttpStatusCode.Conflict, "rehiring a working employee is refused");
        rehired.Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        (await admin.GetUserAsync(scene.Account.Id)).Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue();
        (await admin.GetUnitAsync(scene.Unit.Id)).Json!["headEmployeeId"].Should().BeNull();
        await AssertSignInRefusedAsync(host, scene.Account);
    }

    [Fact]
    public async Task Ac18_EmployeeWithoutAccountAndHeadship_HasAnEmptyImpact()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);

        var impact = (await admin.GetAsync($"/api/v1/employees/{employee.Id}/impact")).Expect(HttpStatusCode.OK);
        var dismissed = await admin.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag);

        impact.Json!["headOfUnits"]!.AsArray().Should().BeEmpty();
        impact.Json["account"].Should().BeNull();
        dismissed.Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac18_StaleDismiss_RollsBackTheWholeCascade()
    {
        var (_, admin, scene) = await ArrangeAsync();
        var bumped = (await admin.PutAsync(
            $"/api/v1/employees/{scene.Employee.Id}",
            new { lastName = scene.Employee.Json!["lastName"]!.GetValue<string>(), firstName = "Павел", middleName = (string?)null, position = "Инженер", orgUnitId = scene.Unit.Id },
            scene.Employee.ETag)).Expect(HttpStatusCode.OK);

        var stale = await admin.PostAsync($"/api/v1/employees/{scene.Employee.Id}/dismiss", ifMatch: scene.Employee.ETag);

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        (await admin.GetUnitAsync(scene.Unit.Id)).Json!["headEmployeeId"]!.GetValue<string>().Should().Be(scene.Employee.Id.ToString());
        (await admin.GetUserAsync(scene.Account.Id)).Json!["isBlocked"]!.GetValue<bool>().Should().BeFalse();
        (await scene.AccountSession.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.OK);
        (await admin.GetEmployeeAsync(scene.Employee.Id)).ETag.Should().Be(bumped.ETag);
    }

    [Fact]
    public async Task Ac19_TransferringTheHead_ReleasesTheHeadshipOfTheUnitTheyLeaveInTheSameRequest()
    {
        var (_, admin, scene) = await ArrangeAsync();
        var target = await admin.CreateUnitAsync();

        var moved = (await admin.PutAsync(
            $"/api/v1/employees/{scene.Employee.Id}",
            new { lastName = scene.Employee.Json!["lastName"]!.GetValue<string>(), firstName = "Иван", middleName = (string?)null, position = "Инженер", orgUnitId = target.Id },
            scene.Employee.ETag)).Expect(HttpStatusCode.OK);

        moved.Json!["orgUnitId"]!.GetValue<string>().Should().Be(target.Id.ToString());
        (await admin.GetUnitAsync(scene.Unit.Id)).Json!["headEmployeeId"].Should().BeNull();
        var journal = await admin.AuditOfRequestAsync(moved.RequestId!);
        journal.Actions().Should().Equal("Employee.Updated", "OrgUnit.Updated");
        var released = journal.Single(row => row!["action"]!.GetValue<string>() == "OrgUnit.Updated")!;
        released["oldValue"]!["headEmployeeId"]!.GetValue<string>().Should().Be(scene.Employee.Id.ToString());
        released["newValue"]!["headEmployeeId"].Should().BeNull();
    }

    private static async Task AssertSignInRefusedAsync(ApiHost host, TestUser account)
    {
        var refused = await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = account.Password });
        refused.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(ApiHost Host, ApiClient Admin, Scene Scene)> ArrangeAsync()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var head = (await admin.SetHeadAsync(unit, employee.Id)).Expect(HttpStatusCode.OK);
        var account = await admin.CreateUserAsync(employeeId: employee.Id);
        return (host, admin, new Scene(head, employee, account, await account.SignInAsync(host)));
    }

    private sealed record Scene(ApiResponse Unit, ApiResponse Employee, TestUser Account, ApiClient AccountSession);
}
