using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// An account with the HR role keeps the organization structure like an administrator does: units, employees with their assignments,
/// dismissals, the position lists and the legal entities. Heads and members still may not, and HR still cannot manage accounts,
/// the journal or the HR assignments, nor dismiss the employee of the last active administrator.
/// </summary>
public sealed class HrOrgStructureTests(CareerHost fixture, TestEnvironment environment) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac8_Hr_MaintainsUnitsEmployeesAndTheirLifecycle()
    {
        var org = await fixture.OrgAsync();
        var hr = org.Hr.Client;
        var parent = await hr.CreateUnitAsync(org.Root.Id);
        var other = await hr.CreateUnitAsync();
        var position = await hr.PositionAsync(parent.Id);
        var entity = await hr.LegalEntityAsync(Scenarios.Unique("ООО"));
        var created = await hr.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Кадров"), "Иван", parent.Id, position, [entity.Id]));
        var lastName = created.Json!["lastName"]!.GetValue<string>();

        (await hr.PutAsync($"/api/v1/org-units/{parent.Id}", new { name = Scenarios.Unique("Новое"), headEmployeeId = created.Id }, parent.ETag)).Expect(HttpStatusCode.OK);
        var edited = (await hr.PutAsync($"/api/v1/employees/{created.Id}", OrgScenes.EmployeeWith(lastName, "Пётр", parent.Id, position, [entity.Id]), created.ETag)).Expect(HttpStatusCode.OK);
        var moved = (await hr.PostAsync(
            $"/api/v1/employees/{created.Id}/move",
            new { fromOrgUnitId = parent.Id, positionId = position, toOrgUnitId = other.Id },
            edited.ETag)).Expect(HttpStatusCode.OK);
        var dismissed = (await hr.PostAsync($"/api/v1/employees/{created.Id}/dismiss", ifMatch: moved.ETag)).Expect(HttpStatusCode.OK);
        var rehired = (await hr.PostAsync($"/api/v1/employees/{created.Id}/rehire", ifMatch: dismissed.ETag)).Expect(HttpStatusCode.OK);
        (await hr.DeleteAsync($"/api/v1/employees/{created.Id}", rehired.ETag)).Expect(HttpStatusCode.NoContent);
        var closed = (await hr.PostAsync($"/api/v1/org-units/{parent.Id}/deactivate", ifMatch: (await hr.GetUnitAsync(parent.Id)).ETag)).Expect(HttpStatusCode.OK);
        (await hr.PostAsync($"/api/v1/org-units/{parent.Id}/activate", ifMatch: closed.ETag)).Expect(HttpStatusCode.OK);
        var slated = await hr.CreateUnitAsync();
        (await hr.DeleteAsync($"/api/v1/org-units/{slated.Id}", slated.ETag)).Expect(HttpStatusCode.NoContent);
        (await hr.PostAsync($"/api/v1/org-units/{parent.Id}/move", new { parentId = other.Id }, (await hr.GetUnitAsync(parent.Id)).ETag)).Expect(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac8_HeadsAndMembers_AreRefusedTheOrganizationMutations()
    {
        var org = await fixture.OrgAsync();
        var unknown = Guid.NewGuid();
        var refused = new List<string>();

        foreach (var (role, client) in new[] { ("head", org.TeamHead.Client), ("member", org.Plain.Client) })
        {
            var calls = new (string Name, Task<ApiResponse> Call)[]
            {
                ("create unit", client.PostAsync("/api/v1/org-units", new { name = Scenarios.Unique("Отдел"), parentId = (Guid?)null })),
                ("create employee", client.PostAsync("/api/v1/employees", Scenarios.EmployeeBody("Иванов", "Иван", unknown, unknown))),
                ("move employee", client.PostAsync($"/api/v1/employees/{org.TeamHead.Employee}/move", new { fromOrgUnitId = org.Team.Id, positionId = unknown, toOrgUnitId = org.Sibling.Id }, "\"1\"")),
                ("delete unit", client.DeleteAsync($"/api/v1/org-units/{org.Sibling.Id}", "\"1\"")),
                ("add legal entity", client.PostAsync("/api/v1/legal-entities/quick-add", new { name = Scenarios.Unique("ООО") })),
                ("import preview", client.PreviewImportAsync(new OrgImportSheet().Unit("Отдел").ToBytes())),
            };
            foreach (var (name, call) in calls)
            {
                var response = await call;
                if (response.Status != HttpStatusCode.Forbidden)
                {
                    refused.Add($"{role}: {name} answered {response.Code}");
                }
            }
        }

        refused.Should().BeEmpty();
    }

    [Fact]
    public async Task Ac8_Hr_StillCannotManageAccountsTheJournalOrTheHrAssignments()
    {
        var org = await fixture.OrgAsync();
        var hr = org.Hr.Client;

        var wrong = new List<string>();
        foreach (var (name, response) in new (string, ApiResponse)[]
        {
            ("users", await hr.GetAsync("/api/v1/users")),
            ("audit", await hr.GetAsync("/api/v1/audit")),
            ("hr candidates", await hr.GetAsync("/api/v1/employees?hrCandidate=true")),
            ("assign hr", await hr.AssignHrAsync(org.Sibling.Id, org.SiblingHr.EmployeeId)),
        })
        {
            if (response.Status != HttpStatusCode.Forbidden)
            {
                wrong.Add($"{name} answered {response.Code}");
            }
        }

        wrong.Should().BeEmpty();
    }

    [Fact]
    public async Task Ac8_Hr_CannotDismissOrDeleteTheEmployeeOfTheLastActiveAdministrator()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var hr = (await admin.HrCandidateAsync(host, unit.Id)).Client;
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        await admin.BindAccountAsync(await admin.OwnIdAsync(), employee.Id);
        var current = await hr.GetEmployeeAsync(employee.Id);

        var dismissal = await hr.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: current.ETag);
        var deletion = await hr.DeleteAsync($"/api/v1/employees/{employee.Id}", current.ETag);

        dismissal.Status.Should().Be(HttpStatusCode.Conflict);
        deletion.Status.Should().Be(HttpStatusCode.Conflict);
        dismissal.Json!["detail"]!.GetValue<string>().Should().Contain("последн");
        (await hr.ViewAsync(employee.Id)).IsActive.Should().BeTrue();
    }
}
