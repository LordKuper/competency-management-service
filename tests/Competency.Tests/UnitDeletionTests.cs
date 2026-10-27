using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Deleting a unit of any status that nothing refers to: its position list and HR assignment go with it in the same operation, the journal records the deletion
/// and keeps the earlier rows, and a unit with children, employees of any assignment or status, or career data is refused with the list of what stops it.
/// </summary>
public sealed class UnitDeletionTests(TestEnvironment environment)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Ac33_Delete_RemovesAnEmptyUnitInAnyStatus_WithItsPositionListAndHrAssignment(bool active)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var elsewhere = await admin.CreateUnitAsync();
        var unit = await admin.CreateUnitAsync();
        await admin.PositionAsync(unit.Id);
        var hr = await admin.HrCandidateAsync(host, elsewhere.Id);
        (await admin.AssignHrAsync(unit.Id, hr.EmployeeId)).Expect(HttpStatusCode.NoContent);
        var current = active ? await admin.GetUnitAsync(unit.Id) : await admin.DeactivateUnitAsync(await admin.GetUnitAsync(unit.Id));

        var deleted = (await admin.DeleteAsync($"/api/v1/org-units/{unit.Id}", current.ETag)).Expect(HttpStatusCode.NoContent);

        (await admin.GetAsync($"/api/v1/org-units/{unit.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
        (await host.ScalarAsync<long>($"SELECT count(*) FROM org_unit_positions WHERE org_unit_id = '{unit.Id}'")).Should().Be(0);
        (await host.ScalarAsync<long>($"SELECT count(*) FROM org_unit_hr WHERE org_unit_id = '{unit.Id}'")).Should().Be(0);
        (await admin.ViewAsync(hr.Employee)).IsActive.Should().BeTrue("the HR assignment goes, the employee stays");
        (await admin.AuditOfRequestAsync(deleted.RequestId!)).Actions().Should().Contain("OrgUnit.Deleted");
    }

    [Fact]
    public async Task Ac33_Delete_KeepsTheEarlierJournalRowsOfTheUnit()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var renamed = (await admin.PutAsync($"/api/v1/org-units/{unit.Id}", new { name = Scenarios.Unique("Новое"), headEmployeeId = (Guid?)null }, unit.ETag)).Expect(HttpStatusCode.OK);
        var before = await host.ScalarAsync<long>($"SELECT count(*) FROM audit_events WHERE entity_id = '{unit.Id}'");

        (await admin.DeleteAsync($"/api/v1/org-units/{unit.Id}", renamed.ETag)).Expect(HttpStatusCode.NoContent);

        before.Should().BeGreaterThanOrEqualTo(2);
        (await host.ScalarAsync<long>($"SELECT count(*) FROM audit_events WHERE entity_id = '{unit.Id}'")).Should().Be(before + 1, "the deletion is one more row and nothing earlier goes");
    }

    [Theory]
    [InlineData("child")]
    [InlineData("inactive-child")]
    [InlineData("main-employee")]
    [InlineData("secondary-employee")]
    [InlineData("dismissed-employee")]
    [InlineData("career-track")]
    public async Task Ac33_Delete_IsRefusedWhileSomethingRefersToTheUnit_NamingWhatStopsIt(string obstacle)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var other = await admin.CreateUnitAsync();
        await admin.PositionAsync(unit.Id);
        await admin.PositionAsync(other.Id);
        var position = await admin.PositionAsync(unit.Id);
        await Arrange(admin, obstacle, unit, other, position);
        var current = await admin.GetUnitAsync(unit.Id);

        var refused = await admin.DeleteAsync($"/api/v1/org-units/{unit.Id}", current.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        refused.Json!["detail"]!.GetValue<string>().Should().Contain(unit.Json!["name"]!.GetValue<string>());
        refused.Json["obstacles"]!.AsArray().Should().ContainSingle().Which!.GetValue<string>().Should().Contain(Wording(obstacle));
        (await admin.GetUnitAsync(unit.Id)).ETag.Should().Be(current.ETag, "a refused deletion changes nothing");
    }

    [Fact]
    public async Task Ac33_Delete_IsRefusedToAUserWithoutTheRole_Is404ForAnUnknownUnit_AndAStaleCopyIs412()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var member = await (await admin.CreateUserAsync()).SignInAsync(host);
        var stale = unit.ETag;
        (await admin.PutAsync($"/api/v1/org-units/{unit.Id}", new { name = Scenarios.Unique("Новое"), headEmployeeId = (Guid?)null }, unit.ETag)).Expect(HttpStatusCode.OK);

        var byMember = await member.DeleteAsync($"/api/v1/org-units/{unit.Id}", stale);
        var unknown = await admin.DeleteAsync($"/api/v1/org-units/{Guid.NewGuid()}", stale);
        var staleDelete = await admin.DeleteAsync($"/api/v1/org-units/{unit.Id}", stale);

        byMember.Status.Should().Be(HttpStatusCode.Forbidden);
        unknown.Status.Should().Be(HttpStatusCode.NotFound);
        staleDelete.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        (await admin.GetAsync($"/api/v1/org-units/{unit.Id}")).Status.Should().Be(HttpStatusCode.OK);
    }

    private static string Wording(string obstacle) => obstacle switch
    {
        "child" or "inactive-child" => "Дочерние подразделения",
        "career-track" => "Треки карьеры",
        _ => "Сотрудники с основным назначением или совмещением",
    };

    private static async Task Arrange(ApiClient admin, string obstacle, ApiResponse unit, ApiResponse other, Guid position)
    {
        switch (obstacle)
        {
            case "child":
                await admin.CreateUnitAsync(unit.Id);
                break;
            case "inactive-child":
                await admin.DeactivateUnitAsync(await admin.CreateUnitAsync(unit.Id));
                break;
            case "main-employee":
                await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Основнов"), "Иван", unit.Id, position));
                break;
            case "secondary-employee":
                await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(
                    Scenarios.Unique("Совмещенов"), "Иван", other.Id, await admin.PositionAsync(other.Id), assignments: [OrgScenes.Assignment(unit.Id, position)]));
                break;
            case "dismissed-employee":
                await admin.DismissAsync(await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Уволенов"), "Иван", unit.Id, position)));
                break;
            default:
                await admin.MakeTrackAsync(unit.Id, (await admin.MakeDirectionAsync()).Id);
                break;
        }
    }
}
