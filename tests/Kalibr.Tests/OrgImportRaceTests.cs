using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The import against everything that changes the same rows at the same time: two applies of one plan, an apply and an edit of an employee it moves,
/// an apply and a block of an administrator, and the dismissal of every administrator in one apply. The requests are lined up on the locks the test holds,
/// so the order in which they proceed is fixed, and each test runs on a host of its own because the lock waiters are counted over the whole database.
/// </summary>
public sealed class OrgImportRaceTests(TestEnvironment environment)
{
    private const string LastAdministratorWording = "последн";

    [Fact]
    public async Task Ac20_TwoAppliesOfOnePlan_ApplyItOnce_AndTheSecondIsToldThePlanChanged()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var name = Scenarios.Unique("Новый");
        var file = new OrgImportSheet().Unit(name).ToBytes();
        var plan = await admin.PlanOfAsync(file);

        await using var held = await RowLock.HoldAdvisoryAsync(host, RowLock.TreeKey);
        var first = await RowLock.StartWaitingAsync(host, 1, () => admin.ApplyKeepingAsync(file, plan));
        var second = await RowLock.StartWaitingAsync(host, 2, () => admin.ApplyKeepingAsync(file, plan));
        await held.ReleaseAsync();
        var results = await Task.WhenAll(first, second);

        results.Select(result => result.Status).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        results.Single(result => result.Status == HttpStatusCode.Conflict).Json!["code"]!.GetValue<string>().Should().Be("planChanged");
        (await host.ScalarAsync<long>($"SELECT count(*) FROM org_units WHERE name = '{name}'")).Should().Be(1, "the unit is created once");
    }

    [Fact]
    public async Task Ac20_AnEditThatCommitsWhileTheApplyWaitsForTheTreeLock_MakesTheApplyRefuseAndChangeNothing()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var scene = await ImportScene.CreateAsync(admin);
        var file = scene.File();
        var plan = await admin.PlanOfAsync(file);

        await using var held = await RowLock.HoldAdvisoryAsync(host, RowLock.TreeKey);
        var apply = await RowLock.StartWaitingAsync(host, 1, () => admin.ApplyKeepingAsync(file, plan));
        await host.ExecuteAsync($"UPDATE employees SET last_name = 'Переименованов' WHERE id = '{scene.Petrov.Id}'");
        await held.ReleaseAsync();
        var result = await apply;

        result.Status.Should().Be(HttpStatusCode.Conflict);
        result.Json!["code"]!.GetValue<string>().Should().Be("planChanged");
        (await host.ScalarAsync<long>($"SELECT count(*) FROM org_units WHERE name = '{scene.NewUnitName}'")).Should().Be(0, "a refused apply creates nothing");
        (await admin.ViewAsync(scene.Sidorov.Id)).IsActive.Should().BeFalse("a refused apply rehires nobody");
        (await admin.ViewAsync(scene.Ivanov.Id)).LegalEntities.Should().Equal(scene.AlphaName);
    }

    [Fact]
    public async Task Ac20_AnApplyAndAnEditOfAnEmployeeItMoves_EndAsOneOrTheOther_NeverAsAMixture()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var scene = await ImportScene.CreateAsync(admin);
        var file = scene.File();
        var plan = await admin.PlanOfAsync(file);

        await using var held = await RowLock.HoldAdvisoryAsync(host, RowLock.TreeKey);
        var apply = await RowLock.StartWaitingAsync(host, 1, () => admin.ApplyKeepingAsync(file, plan));
        var edit = await RowLock.StartWaitingAsync(host, 2, () => admin.PutAsync(
            $"/api/v1/employees/{scene.Petrov.Id}",
            Scenarios.EmployeeBody(scene.Petrov.Json!["lastName"]!.GetValue<string>(), "Павел", scene.Engineer, scene.Root.Id),
            scene.Petrov.ETag));
        await held.ReleaseAsync();
        var results = await Task.WhenAll(apply, edit);

        var newUnits = await host.ScalarAsync<long>($"SELECT count(*) FROM org_units WHERE name = '{scene.NewUnitName}'");
        var petrov = await admin.ViewAsync(scene.Petrov.Id);
        if (results[0].Status == HttpStatusCode.OK)
        {
            results[1].Status.Should().Be(HttpStatusCode.PreconditionFailed, "the apply changed the employee the edit was made on");
            newUnits.Should().Be(1);
            petrov.Unit.Should().Be(scene.NewUnitName);
        }
        else
        {
            results[0].Status.Should().Be(HttpStatusCode.Conflict);
            results[1].Status.Should().Be(HttpStatusCode.OK);
            newUnits.Should().Be(0);
            petrov.Unit.Should().Be(scene.RootName);
            (await admin.ViewAsync(scene.Sidorov.Id)).IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Ac18_ADismissalByImportAndABlockOfTheOtherAdministrator_LeaveExactlyOneActiveAdministrator()
    {
        await using var host = await environment.StartHostAsync();
        var org = await TwoAdministratorsAsync(host);
        var plan = await org.First.PlanOfAsync(org.FileNamingTheFirst);
        var versionOfFirst = (await org.Second.GetUserAsync(org.FirstId)).ETag;

        await using var held = await RowLock.HoldActiveAdministratorsAsync(host);
        var import = await RowLock.StartWaitingAsync(host, 1, () => org.First.ApplyKeepingAsync(org.FileNamingTheFirst, plan, dismiss: [org.SecondEmployee]));
        var block = await RowLock.StartWaitingAsync(host, 2, () => org.Second.PostAsync($"/api/v1/users/{org.FirstId}/block", ifMatch: versionOfFirst));
        await held.ReleaseAsync();
        var results = await Task.WhenAll(import, block);

        results[0].Status.Should().Be(HttpStatusCode.OK, results[0].Body);
        results[1].Status.Should().Be(HttpStatusCode.Conflict, "the import took the administrators first and left the first as the only active one: " + results[1].Body);
        results[1].Json!["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        (await org.First.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.OK);
        (await host.ScalarAsync<long>("SELECT count(*) FROM users WHERE NOT is_blocked AND password_hash IS NOT NULL")).Should().Be(1);
    }

    [Fact]
    public async Task Ac18_Apply_ThatWouldDismissTheEmployeesOfEveryActiveAdministrator_IsRefused_AndChangesNothing()
    {
        await using var host = await environment.StartHostAsync();
        var org = await TwoAdministratorsAsync(host);
        var name = Scenarios.Unique("Новый");
        var file = new OrgImportSheet().Unit(name).ToBytes();
        var plan = await org.First.PlanOfAsync(file);

        var refused = await org.First.ApplyImportAsync(file, plan.FingerprintOf());

        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        refused.Json!["code"].Should().BeNull();
        refused.Json["detail"]!.GetValue<string>().Should().Contain(LastAdministratorWording);
        (await org.First.ViewAsync(org.FirstEmployee)).IsActive.Should().BeTrue();
        (await org.First.ViewAsync(org.SecondEmployee)).IsActive.Should().BeTrue();
        (await org.First.PlanOfAsync(file)).Lines("newUnits").Should().Equal([name], "nothing was created");
    }

    [Fact]
    public async Task Ac18_Apply_ThatDismissesTheEmployeeOfOneOfTwoAdministrators_Succeeds_AndBlocksTheirAccountOnly()
    {
        await using var host = await environment.StartHostAsync();
        var org = await TwoAdministratorsAsync(host);
        var plan = await org.First.PlanOfAsync(org.FileNamingTheFirst);

        var applied = await org.First.ApplyKeepingAsync(org.FileNamingTheFirst, plan, dismiss: [org.SecondEmployee]);

        applied.Status.Should().Be(HttpStatusCode.OK, applied.Body);
        (await org.First.GetUserAsync(org.SecondId)).Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue();
        (await org.First.GetUserAsync(org.FirstId)).Json!["isBlocked"]!.GetValue<bool>().Should().BeFalse();
        (await org.Second.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<Administrators> TwoAdministratorsAsync(ApiHost host)
    {
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var first = await admin.CreateEmployeeAsync(unit.Id, Scenarios.Unique("Первов"));
        var second = await admin.CreateEmployeeAsync(unit.Id, Scenarios.Unique("Второв"));
        var firstId = await admin.OwnIdAsync();
        var secondAccount = await admin.CreateUserAsync(Scenarios.GlobalAdmin, second.Id);
        await admin.BindAccountAsync(firstId, first.Id);
        var firstSession = await host.LoginAsync(ApiHost.AdminEmail, ApiHost.AdminPassword);
        var secondSession = await secondAccount.SignInAsync(host);
        var file = new OrgImportSheet()
            .Employee(first.Json!["fullName"]!.GetValue<string>(), "Инженер", unit.Json!["name"]!.GetValue<string>())
            .ToBytes();
        return new Administrators(firstSession, secondSession, firstId, secondAccount.Id, first.Id, second.Id, file);
    }

    private sealed record Administrators(
        ApiClient First,
        ApiClient Second,
        Guid FirstId,
        Guid SecondId,
        Guid FirstEmployee,
        Guid SecondEmployee,
        byte[] FileNamingTheFirst);
}
