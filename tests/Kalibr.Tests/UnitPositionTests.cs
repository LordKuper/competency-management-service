using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The positions allowed in a unit: administrators allow catalogue positions in a unit and take them off, a unit also offers the positions of the
/// units above it with their source, the number of employees an action would touch is shown first, and the employee form can add a position
/// to the catalogue and to the unit in one step.
/// </summary>
public sealed class UnitPositionTests(TestEnvironment environment)
{
    [Fact]
    public async Task Ac2_Add_AllowsAnActivePositionInTheUnit_AndAllowingItAgainChangesNothing()
    {
        var (admin, _) = await ArrangeAsync();
        var unit = await admin.CreateUnitAsync();
        var position = await CreatePositionAsync(admin);

        var first = await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions", new { positionId = position.Id });
        var again = await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions", new { positionId = position.Id });

        first.Status.Should().Be(HttpStatusCode.NoContent);
        again.Status.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(admin, unit.Id)).Select(Describe).Should().Equal($"{position.Json!["name"]!.GetValue<string>()} / {unit.Id}");
    }

    [Fact]
    public async Task Ac2_Add_RefusesAnInactiveAnUnknownPositionAndAnUnknownUnit()
    {
        var (admin, _) = await ArrangeAsync();
        var unit = await admin.CreateUnitAsync();
        var inactive = await CreatePositionAsync(admin);
        (await admin.PostAsync($"/api/v1/positions/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);
        var position = await CreatePositionAsync(admin);

        var refusedInactive = await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions", new { positionId = inactive.Id });
        var refusedUnknown = await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions", new { positionId = Guid.NewGuid() });
        var unknownUnit = await admin.PostAsync($"/api/v1/org-units/{Guid.NewGuid()}/positions", new { positionId = position.Id });

        refusedInactive.Status.Should().Be(HttpStatusCode.BadRequest);
        refusedInactive.FieldErrors("positionId").Should().Equal("Должность неактивна");
        refusedUnknown.Status.Should().Be(HttpStatusCode.BadRequest);
        refusedUnknown.FieldErrors("positionId").Should().Equal("Должность не найдена");
        unknownUnit.Status.Should().Be(HttpStatusCode.NotFound);
        (await ListAsync(admin, unit.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Ac2_List_ShowsTheOwnAndTheInheritedPositionsWithTheirSource_AListedTwiceAppearsOncePerList()
    {
        var (admin, _) = await ArrangeAsync();
        var parent = await admin.CreateUnitAsync();
        var child = await admin.CreateUnitAsync(parent.Id);
        var grandchild = await admin.CreateUnitAsync(child.Id);
        var token = Scenarios.Unique("t")[2..];
        var first = await CreatePositionAsync(admin, $"А {token}");
        var second = await CreatePositionAsync(admin, $"Б {token}");
        var third = await CreatePositionAsync(admin, $"В {token}");
        await AllowAsync(admin, parent.Id, first, second);
        await AllowAsync(admin, child.Id, second, third);

        var ofChild = await ListAsync(admin, child.Id);
        var ofGrandchild = (await ListAsync(admin, grandchild.Id)).Select(Describe).ToList();

        ofChild.Select(row => row!["name"]!.GetValue<string>()).Should().Equal([$"А {token}", $"Б {token}", $"Б {token}", $"В {token}"], "the list is in name order");
        ofChild.Select(row => $"{Describe(row)} / {row!["orgUnitName"]!.GetValue<string>()}").Should().BeEquivalentTo(
            [
                $"А {token} / {parent.Id} / {parent.Json!["name"]!.GetValue<string>()}",
                $"Б {token} / {child.Id} / {child.Json!["name"]!.GetValue<string>()}",
                $"Б {token} / {parent.Id} / {parent.Json!["name"]!.GetValue<string>()}",
                $"В {token} / {child.Id} / {child.Json!["name"]!.GetValue<string>()}",
            ],
            "the order of one position listed by two units follows the unit names, which are random here, so only the rows are compared");
        ofGrandchild.Should().HaveCount(4, "the unit's own list is empty and the lists above it come whole");
        (await ListAsync(admin, parent.Id)).Select(Describe).Should().Equal([$"А {token} / {parent.Id}", $"Б {token} / {parent.Id}"], "the units below do not pass their positions up");
    }

    [Fact]
    public async Task Ac2_List_HidesDeactivatedPositionsFromUsers_AndADeactivatedUnitFromThemToo()
    {
        var (admin, host) = await ArrangeAsync();
        var user = (await admin.PlainMemberAsync(host)).Client;
        var unit = await admin.CreateUnitAsync();
        var kept = await CreatePositionAsync(admin);
        var gone = await CreatePositionAsync(admin);
        await AllowAsync(admin, unit.Id, kept, gone);
        (await admin.PostAsync($"/api/v1/positions/{gone.Id}/deactivate", ifMatch: gone.ETag)).Expect(HttpStatusCode.OK);
        var closed = await admin.CreateUnitAsync();
        (await admin.PostAsync($"/api/v1/org-units/{closed.Id}/deactivate", ifMatch: closed.ETag)).Expect(HttpStatusCode.OK);

        var seenByUser = await ListAsync(user, unit.Id);
        var seenByAdmin = await ListAsync(admin, unit.Id);
        var inactiveUnit = await user.GetAsync($"/api/v1/org-units/{closed.Id}/positions");

        seenByUser.Select(row => row!["positionId"]!.GetValue<string>()).Should().Equal(kept.Id.ToString());
        seenByAdmin.Should().HaveCount(2);
        seenByAdmin.Single(row => row!["positionId"]!.GetValue<string>() == gone.Id.ToString())!["isActive"]!.GetValue<bool>().Should().BeFalse();
        inactiveUnit.Status.Should().Be(HttpStatusCode.NotFound);
        (await admin.GetAsync($"/api/v1/org-units/{closed.Id}/positions")).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac2_Remove_TakesThePositionOffTheUnitsOwnList_AndTheEmployeesWhoHoldItKeepIt()
    {
        var (admin, _) = await ArrangeAsync();
        var parent = await admin.CreateUnitAsync();
        var child = await admin.CreateUnitAsync(parent.Id);
        var name = Scenarios.Unique("Мастер");
        var employee = await admin.CreateEmployeeAsync(child.Id, position: name);
        var positionId = employee.Json!["positionId"]!.GetValue<Guid>();
        var inherited = await CreatePositionAsync(admin);
        await AllowAsync(admin, parent.Id, inherited);

        var removed = await admin.DeleteAsync($"/api/v1/org-units/{child.Id}/positions/{positionId}");
        var removedAgain = await admin.DeleteAsync($"/api/v1/org-units/{child.Id}/positions/{positionId}");
        var removedInherited = await admin.DeleteAsync($"/api/v1/org-units/{child.Id}/positions/{inherited.Id}");

        removed.Status.Should().Be(HttpStatusCode.NoContent);
        removedAgain.Status.Should().Be(HttpStatusCode.NotFound);
        removedInherited.Status.Should().Be(HttpStatusCode.NotFound, "a unit only takes off what its own list holds");
        (await ListAsync(admin, child.Id)).Select(row => row!["positionId"]!.GetValue<string>()).Should().Equal(inherited.Id.ToString());
        (await admin.GetEmployeeAsync(employee.Id)).Json!["position"]!.GetValue<string>().Should().Be(name);
    }

    [Fact]
    public async Task Ac2_Impact_CountsTheWorkingEmployeesOfTheUnitAndTheUnitsBelowWhoHoldThePosition()
    {
        var (admin, _) = await ArrangeAsync();
        var parent = await admin.CreateUnitAsync();
        var child = await admin.CreateUnitAsync(parent.Id);
        var other = await admin.CreateUnitAsync();
        var name = Scenarios.Unique("Технолог");
        var inParent = await admin.CreateEmployeeAsync(parent.Id, position: name);
        await admin.CreateEmployeeAsync(child.Id, position: name);
        var leaver = await admin.CreateEmployeeAsync(child.Id, position: name);
        await admin.CreateEmployeeAsync(child.Id);
        await admin.CreateEmployeeAsync(other.Id, position: name);
        (await admin.PostAsync($"/api/v1/employees/{leaver.Id}/dismiss", ifMatch: leaver.ETag)).Expect(HttpStatusCode.OK);
        var positionId = inParent.Json!["positionId"]!.GetValue<Guid>();

        var ofParent = (await admin.GetAsync($"/api/v1/org-units/{parent.Id}/positions/{positionId}/impact")).Expect(HttpStatusCode.OK);
        var ofChild = (await admin.GetAsync($"/api/v1/org-units/{child.Id}/positions/{positionId}/impact")).Expect(HttpStatusCode.OK);
        var notOnTheList = await admin.GetAsync($"/api/v1/org-units/{parent.Id}/positions/{Guid.NewGuid()}/impact");

        ofParent.Json!["employeeCount"]!.GetValue<int>().Should().Be(2, "the working employees of the unit and of the unit below, not the dismissed one or the other tree's");
        ofChild.Json!["employeeCount"]!.GetValue<int>().Should().Be(1);
        notOnTheList.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac4_QuickAdd_CreatesThePositionAndAllowsItInTheUnitInOneStep_WithTheNameTrimmed()
    {
        var (admin, _) = await ArrangeAsync();
        var unit = await admin.CreateUnitAsync();
        var name = Scenarios.Unique("Новая");

        var added = (await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions/quick-add", new { name = $"  {name} " })).Expect(HttpStatusCode.OK);

        added.Json!["name"]!.GetValue<string>().Should().Be(name);
        added.Json["isActive"]!.GetValue<bool>().Should().BeTrue();
        added.ETag.Should().NotBeNull();
        (await ListAsync(admin, unit.Id)).Select(Describe).Should().Equal($"{name} / {unit.Id}");
        (await admin.GetAsync($"/api/v1/positions/{added.Id}")).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac4_QuickAdd_TakesTheExistingPositionWithTheSameNameRegardlessOfCase_KeepingItsSpelling_AndAllowsItInTheUnit()
    {
        var (admin, _) = await ArrangeAsync();
        var unit = await admin.CreateUnitAsync();
        var existing = await CreatePositionAsync(admin);
        var name = existing.Json!["name"]!.GetValue<string>();

        var added = (await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions/quick-add", new { name = $" {name.ToUpperInvariant()} " })).Expect(HttpStatusCode.OK);

        added.Id.Should().Be(existing.Id);
        added.Json!["name"]!.GetValue<string>().Should().Be(name);
        (await ListAsync(admin, unit.Id)).Select(Describe).Should().Equal($"{name} / {unit.Id}");
    }

    [Fact]
    public async Task Ac4_QuickAdd_BringsAnInactivePositionBackToWork_AndJournalsBothChanges()
    {
        var (admin, _) = await ArrangeAsync();
        var unit = await admin.CreateUnitAsync();
        var position = await CreatePositionAsync(admin);
        (await admin.PostAsync($"/api/v1/positions/{position.Id}/deactivate", ifMatch: position.ETag)).Expect(HttpStatusCode.OK);

        var added = (await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions/quick-add", new { name = position.Json!["name"]!.GetValue<string>() })).Expect(HttpStatusCode.OK);

        added.Id.Should().Be(position.Id);
        added.Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        (await admin.AuditOfRequestAsync(added.RequestId!)).Actions().Should().Equal("OrgUnitPosition.Created", "Position.Updated");
    }

    [Fact]
    public async Task Ac4_QuickAdd_ForAnUnknownUnitOrWithAnInvalidName_ChangesNothing()
    {
        var (admin, _) = await ArrangeAsync();
        var unit = await admin.CreateUnitAsync();
        var name = Scenarios.Unique("Призрак");

        var unknownUnit = await admin.PostAsync($"/api/v1/org-units/{Guid.NewGuid()}/positions/quick-add", new { name });
        var blank = await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions/quick-add", new { name = " " });
        var tooLong = await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions/quick-add", new { name = new string('я', 201) });

        unknownUnit.Status.Should().Be(HttpStatusCode.NotFound);
        blank.FieldErrors("name").Should().Equal("Укажите название");
        tooLong.FieldErrors("name").Should().Equal("Не длиннее 200 символов");
        (await admin.GetAsync($"/api/v1/positions?q={name}")).Json!["total"]!.GetValue<int>().Should().Be(0, "a refused request leaves no position behind");
        (await ListAsync(admin, unit.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Ac6_Journal_RecordsAllowingAPositionInAUnitAndTakingItOff()
    {
        var (admin, _) = await ArrangeAsync();
        var unit = await admin.CreateUnitAsync();
        var position = await CreatePositionAsync(admin);

        var allowed = (await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions", new { positionId = position.Id })).Expect(HttpStatusCode.NoContent);
        var removed = (await admin.DeleteAsync($"/api/v1/org-units/{unit.Id}/positions/{position.Id}")).Expect(HttpStatusCode.NoContent);

        var creation = (await admin.AuditOfRequestAsync(allowed.RequestId!)).Single()!;
        var deletion = (await admin.AuditOfRequestAsync(removed.RequestId!)).Single()!;
        creation["action"]!.GetValue<string>().Should().Be("OrgUnitPosition.Created");
        creation["newValue"]!["orgUnitId"]!.GetValue<string>().Should().Be(unit.Id.ToString());
        creation["newValue"]!["positionId"]!.GetValue<string>().Should().Be(position.Id.ToString());
        deletion["action"]!.GetValue<string>().Should().Be("OrgUnitPosition.Deleted");
        deletion["oldValue"]!["positionId"]!.GetValue<string>().Should().Be(position.Id.ToString());
    }

    private static string Describe(JsonNode? row) => $"{row!["name"]!.GetValue<string>()} / {row!["orgUnitId"]!.GetValue<string>()}";

    private static async Task AllowAsync(ApiClient admin, Guid unitId, params ApiResponse[] positions)
    {
        foreach (var position in positions)
        {
            (await admin.PostAsync($"/api/v1/org-units/{unitId}/positions", new { positionId = position.Id })).Expect(HttpStatusCode.NoContent);
        }
    }

    private static async Task<List<JsonNode?>> ListAsync(ApiClient client, Guid unitId) =>
        [.. (await client.GetAsync($"/api/v1/org-units/{unitId}/positions")).Expect(HttpStatusCode.OK).Json!.AsArray()];

    private static async Task<ApiResponse> CreatePositionAsync(ApiClient admin, string? name = null) =>
        (await admin.PostAsync("/api/v1/positions", new { name = name ?? Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);

    private async Task<(ApiClient Admin, ApiHost Host)> ArrangeAsync()
    {
        var host = await environment.SharedHostAsync();
        return (await host.AdminAsync(), host);
    }
}
