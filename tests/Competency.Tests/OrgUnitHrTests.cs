using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The HR of a unit: administrators assign one employee to a unit and remove the assignment, the unit and the units below it inherit it down to
/// the first unit with an assignment of its own, an employee whose account cannot act leaves the unit without an HR, and every assignment is journaled.
/// </summary>
public sealed class OrgUnitHrTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string CannotActMessage = "Учётная запись заблокирована или не завершила регистрацию";
    private const string NoAccountMessage = "К сотруднику не привязана учётная запись";
    private const string NoHrRoleMessage = "У учётной записи нет роли «HR»";

    [Fact]
    public async Task Ac8_Hr_IsTheOneAssignedToTheUnitOrElseTheNearestAboveIt_AndEveryUserSeesIt()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var ofTeam = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Team.Id}/hr")).Expect(HttpStatusCode.OK);
        var ofSibling = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.OK);

        ofTeam.Json!["assigned"].Should().BeNull();
        ofTeam.Json["inherited"]!["employeeId"]!.GetValue<Guid>().Should().Be(org.Hr.Employee);
        ofTeam.Json["inherited"]!["orgUnitId"]!.GetValue<Guid>().Should().Be(org.Root.Id);
        ofTeam.Json["inherited"]!["orgUnitName"]!.GetValue<string>().Should().Be(org.Root.Json!["name"]!.GetValue<string>());
        ofSibling.Json!["assigned"]!["employeeId"]!.GetValue<Guid>().Should().Be(org.SiblingHr.Employee);
        ofSibling.Json["inherited"]!["employeeId"]!.GetValue<Guid>().Should().Be(org.Hr.Employee, "the inherited one would take over once the own assignment is removed");
    }

    [Fact]
    public async Task Ac8_Hr_IsNamedByTheEmployeesFullName()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var unit = await org.Admin.CreateUnitAsync(org.Root.Id);
        var employee = await org.Admin.CreateEmployeeAsync(unit.Id, lastName: "Кузнецов", firstName: "Пётр", middleName: "Иванович");
        await org.Admin.CreateUserAsync(Scenarios.Hr, employee.Id);

        (await org.Admin.AssignHrAsync(unit.Id, employee.Id)).Expect(HttpStatusCode.NoContent);

        var assigned = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{unit.Id}/hr")).Expect(HttpStatusCode.OK).Json!["assigned"]!;
        assigned["name"]!.GetValue<string>().Should().Be("Кузнецов Пётр Иванович");
    }

    [Fact]
    public async Task Ac8_Assign_ReplacesTheUnitsPreviousAssignment_AndAssigningTheSameEmployeeAgainChangesNothing()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var other = await org.Admin.HrCandidateAsync(fixture.Host, org.Sibling.Id);

        var replaced = await org.Admin.AssignHrAsync(org.Sibling.Id, other.EmployeeId);
        var again = await org.Admin.AssignHrAsync(org.Sibling.Id, other.EmployeeId);

        replaced.Status.Should().Be(HttpStatusCode.NoContent);
        again.Status.Should().Be(HttpStatusCode.NoContent);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM org_unit_hr WHERE org_unit_id = '{org.Sibling.Id}'")).Should().Be(1, "a unit has one HR");
        (await org.Admin.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Json!["assigned"]!["employeeId"]!.GetValue<Guid>().Should().Be(other.Employee);
        (await org.Admin.AuditOfRequestAsync(again.RequestId!)).Should().BeEmpty("assigning the same employee again is not a change");
    }

    [Fact]
    public async Task Ac8_Assign_RefusesAnEmployeeWhoseAccountCannotAct_AnUnknownEmployeeAndAnUnknownUnit()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var invited = await org.Admin.CreateEmployeeAsync(org.Team.Id);
        await org.Admin.InviteUserAsync(Scenarios.Hr, invited.Id);
        var blocked = await org.Admin.HrCandidateAsync(fixture.Host, org.Team.Id);
        (await org.Admin.PostAsync($"/api/v1/users/{blocked.Id}/block", ifMatch: (await org.Admin.GetUserAsync(blocked.Id)).ETag)).Expect(HttpStatusCode.OK);
        var withoutAccount = await org.Admin.CreateEmployeeAsync(org.Team.Id);
        var withoutRole = await org.Admin.CreateEmployeeAsync(org.Team.Id);
        await org.Admin.CreateUserAsync(employeeId: withoutRole.Id);
        var regular = await org.Admin.HrCandidateAsync(fixture.Host, org.Team.Id);

        var notRegistered = await org.Admin.AssignHrAsync(org.Team.Id, invited.Id);
        var isBlocked = await org.Admin.AssignHrAsync(org.Team.Id, blocked.EmployeeId);
        var noAccount = await org.Admin.AssignHrAsync(org.Team.Id, withoutAccount.Id);
        var noRole = await org.Admin.AssignHrAsync(org.Team.Id, withoutRole.Id);
        var unknown = await org.Admin.AssignHrAsync(org.Team.Id, Guid.NewGuid());
        var unknownUnit = await org.Admin.AssignHrAsync(Guid.NewGuid(), regular.EmployeeId);

        notRegistered.FieldErrors("employeeId").Should().Equal(CannotActMessage);
        isBlocked.FieldErrors("employeeId").Should().Equal(CannotActMessage);
        noAccount.FieldErrors("employeeId").Should().Equal(NoAccountMessage);
        noRole.FieldErrors("employeeId").Should().Equal(NoHrRoleMessage);
        unknown.FieldErrors("employeeId").Should().Equal("Сотрудник не найден");
        unknownUnit.Status.Should().Be(HttpStatusCode.NotFound);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM org_unit_hr WHERE org_unit_id = '{org.Team.Id}'")).Should().Be(0);
    }

    [Fact]
    public async Task Ac8_Remove_DeletesTheUnitsAssignment_SoTheUnitInheritsAgain_AndRemovingNothingIsNotFound()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var removed = await org.Admin.DeleteAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr");
        var again = await org.Admin.DeleteAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr");

        removed.Status.Should().Be(HttpStatusCode.NoContent);
        again.Status.Should().Be(HttpStatusCode.NotFound);
        var hr = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.OK).Json!;
        hr["assigned"].Should().BeNull();
        hr["inherited"]!["employeeId"]!.GetValue<Guid>().Should().Be(org.Hr.Employee);
        (await org.SiblingHr.Client.GetAsync("/api/v1/career/me")).Json!["hrUnits"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Ac8_OneEmployee_CanBeAssignedToSeveralUnits()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        (await org.Admin.AssignHrAsync(org.Team.Id, org.SiblingHr.EmployeeId)).Expect(HttpStatusCode.NoContent);

        var me = (await org.SiblingHr.Client.GetAsync("/api/v1/career/me")).Expect(HttpStatusCode.OK);
        me.Json!["hrUnits"]!.AsArray().Select(unit => unit!["id"]!.GetValue<Guid>()).Should().BeEquivalentTo([org.Team.Id, org.Sibling.Id]);
    }

    [Fact]
    public async Task Ac8_AnEmployeeWhoseAccountCannotAct_LeavesTheUnitWithoutAnHr_ForEveryoneButAdministrators()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var siblingHr = await org.Admin.GetUserAsync(org.SiblingHr.Id);
        (await org.Admin.PostAsync($"/api/v1/users/{org.SiblingHr.Id}/block", ifMatch: siblingHr.ETag)).Expect(HttpStatusCode.OK);

        var forUser = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.OK).Json!;
        var forAdmin = (await org.Admin.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.OK).Json!;
        var ofTeamForUser = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Team.Id}/hr")).Expect(HttpStatusCode.OK).Json!;

        forUser["assigned"].Should().BeNull();
        forUser["inherited"].Should().BeNull("the role is not passed further up while the unit's own HR cannot act");
        forAdmin["assigned"]!["account"]!["state"]!.GetValue<string>().Should().Be("Blocked");
        forAdmin["inherited"]!["account"]!["state"]!.GetValue<string>().Should().Be("Active");
        ofTeamForUser["inherited"]!["employeeId"]!.GetValue<Guid>().Should().Be(org.Hr.Employee, "a unit with no assignment of its own is not affected");
        ofTeamForUser["inherited"]!["account"].Should().BeNull("only administrators see the state of the account");
    }

    [Fact]
    public async Task Ac8_AnInactiveUnit_IsNotFoundForAUser_ButNotForAnAdministrator()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var closed = await org.Admin.CreateUnitAsync(org.Root.Id);
        (await org.Admin.PostAsync($"/api/v1/org-units/{closed.Id}/deactivate", ifMatch: closed.ETag)).Expect(HttpStatusCode.OK);

        var asUser = await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{closed.Id}/hr");
        var asAdmin = await org.Admin.GetAsync($"/api/v1/career/org-units/{closed.Id}/hr");
        var unknown = await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{Guid.NewGuid()}/hr");

        asUser.Status.Should().Be(HttpStatusCode.NotFound);
        asAdmin.Status.Should().Be(HttpStatusCode.OK);
        unknown.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac8_OnlyAdministrators_AssignAndRemove()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        foreach (var member in new[] { org.Plain, org.TeamHead, org.Hr })
        {
            var assign = await member.Client.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { employeeId = org.Hr.EmployeeId });
            var remove = await member.Client.DeleteAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr");

            assign.Status.Should().Be(HttpStatusCode.Forbidden, member.User.Email);
            remove.Status.Should().Be(HttpStatusCode.Forbidden, member.User.Email);
        }

        (await org.Admin.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Json!["assigned"].Should().NotBeNull();
    }

    [Fact]
    public async Task Ac8_Journal_RecordsAssigningReplacingAndRemoving()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var first = await org.Admin.HrCandidateAsync(fixture.Host, org.Team.Id);
        var second = await org.Admin.HrCandidateAsync(fixture.Host, org.Team.Id);

        var assigned = (await org.Admin.AssignHrAsync(org.Team.Id, first.EmployeeId)).Expect(HttpStatusCode.NoContent);
        var replaced = (await org.Admin.AssignHrAsync(org.Team.Id, second.EmployeeId)).Expect(HttpStatusCode.NoContent);
        var removed = (await org.Admin.DeleteAsync($"/api/v1/career/org-units/{org.Team.Id}/hr")).Expect(HttpStatusCode.NoContent);

        var creation = (await org.Admin.AuditOfRequestAsync(assigned.RequestId!)).Single()!;
        var change = (await org.Admin.AuditOfRequestAsync(replaced.RequestId!)).Single()!;
        var deletion = (await org.Admin.AuditOfRequestAsync(removed.RequestId!)).Single()!;
        creation["action"]!.GetValue<string>().Should().Be("OrgUnitHr.Created");
        creation["newValue"]!["employeeId"]!.GetValue<Guid>().Should().Be(first.Employee);
        change["action"]!.GetValue<string>().Should().Be("OrgUnitHr.Updated");
        change["oldValue"]!["employeeId"]!.GetValue<Guid>().Should().Be(first.Employee);
        change["newValue"]!["employeeId"]!.GetValue<Guid>().Should().Be(second.Employee);
        deletion["action"]!.GetValue<string>().Should().Be("OrgUnitHr.Deleted");
        deletion["oldValue"]!["employeeId"]!.GetValue<Guid>().Should().Be(second.Employee);
    }
}
