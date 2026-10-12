using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The HR of a unit: administrators assign one account to a unit and remove the assignment, the unit and the units below it inherit it down to
/// the first unit with an assignment of its own, an account that cannot act leaves the unit without an HR, and every assignment is journaled.
/// </summary>
public sealed class OrgUnitHrTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string CannotActMessage = "Учётная запись заблокирована или не завершила регистрацию";

    [Fact]
    public async Task Ac8_Hr_IsTheOneAssignedToTheUnitOrElseTheNearestAboveIt_AndEveryUserSeesIt()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var ofTeam = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Team.Id}/hr")).Expect(HttpStatusCode.OK);
        var ofSibling = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.OK);

        ofTeam.Json!["assigned"].Should().BeNull();
        ofTeam.Json["inherited"]!["userId"]!.GetValue<Guid>().Should().Be(org.Hr.Id);
        ofTeam.Json["inherited"]!["orgUnitId"]!.GetValue<Guid>().Should().Be(org.Root.Id);
        ofTeam.Json["inherited"]!["orgUnitName"]!.GetValue<string>().Should().Be(org.Root.Json!["name"]!.GetValue<string>());
        ofTeam.Json["inherited"]!["name"]!.GetValue<string>().Should().Be(org.Hr.User.Email, "an account bound to no employee is named by its e-mail");
        ofSibling.Json!["assigned"]!["userId"]!.GetValue<Guid>().Should().Be(org.SiblingHr.Id);
        ofSibling.Json["inherited"]!["userId"]!.GetValue<Guid>().Should().Be(org.Hr.Id, "the inherited one would take over once the own assignment is removed");
    }

    [Fact]
    public async Task Ac8_HrAccountBoundToAnEmployee_IsNamedByTheEmployeesFullName()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var unit = await org.Admin.CreateUnitAsync(org.Root.Id);
        var employee = await org.Admin.CreateEmployeeAsync(unit.Id, lastName: "Кузнецов", firstName: "Пётр", middleName: "Иванович");
        var account = await org.Admin.CreateUserAsync(employeeId: employee.Id);

        (await org.Admin.PutAsync($"/api/v1/career/org-units/{unit.Id}/hr", new { userId = account.Id })).Expect(HttpStatusCode.NoContent);

        var assigned = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{unit.Id}/hr")).Expect(HttpStatusCode.OK).Json!["assigned"]!;
        assigned["name"]!.GetValue<string>().Should().Be("Кузнецов Пётр Иванович");
    }

    [Fact]
    public async Task Ac8_Assign_ReplacesTheUnitsPreviousAssignment_AndAssigningTheSameAccountAgainChangesNothing()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var other = await org.Admin.CreateUserAsync();

        var replaced = await org.Admin.PutAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr", new { userId = other.Id });
        var again = await org.Admin.PutAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr", new { userId = other.Id });

        replaced.Status.Should().Be(HttpStatusCode.NoContent);
        again.Status.Should().Be(HttpStatusCode.NoContent);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM org_unit_hr WHERE org_unit_id = '{org.Sibling.Id}'")).Should().Be(1, "a unit has one HR");
        (await org.Admin.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Json!["assigned"]!["userId"]!.GetValue<Guid>().Should().Be(other.Id);
        (await org.Admin.AuditOfRequestAsync(again.RequestId!)).Should().BeEmpty("assigning the same account again is not a change");
    }

    [Fact]
    public async Task Ac8_Assign_RefusesAnAccountThatCannotAct_AnUnknownAccountAndAnUnknownUnit()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var invited = await org.Admin.InviteUserAsync();
        var blocked = await org.Admin.CreateUserAsync();
        (await org.Admin.PostAsync($"/api/v1/users/{blocked.Id}/block", ifMatch: blocked.ETag)).Expect(HttpStatusCode.OK);
        var regular = await org.Admin.CreateUserAsync();

        var notRegistered = await org.Admin.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { userId = invited.Id });
        var isBlocked = await org.Admin.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { userId = blocked.Id });
        var unknown = await org.Admin.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { userId = Guid.NewGuid() });
        var unknownUnit = await org.Admin.PutAsync($"/api/v1/career/org-units/{Guid.NewGuid()}/hr", new { userId = regular.Id });

        notRegistered.FieldErrors("userId").Should().Equal(CannotActMessage);
        isBlocked.FieldErrors("userId").Should().Equal(CannotActMessage);
        unknown.FieldErrors("userId").Should().Equal("Пользователь не найден");
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
        hr["inherited"]!["userId"]!.GetValue<Guid>().Should().Be(org.Hr.Id);
        (await org.SiblingHr.Client.GetAsync("/api/v1/career/me")).Json!["hrUnits"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Ac8_OneAccount_CanBeAssignedToSeveralUnits()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        (await org.Admin.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { userId = org.SiblingHr.Id })).Expect(HttpStatusCode.NoContent);

        var me = (await org.SiblingHr.Client.GetAsync("/api/v1/career/me")).Expect(HttpStatusCode.OK);
        me.Json!["hrUnits"]!.AsArray().Select(unit => unit!["id"]!.GetValue<Guid>()).Should().BeEquivalentTo([org.Team.Id, org.Sibling.Id]);
    }

    [Fact]
    public async Task Ac8_AnAccountThatCannotAct_LeavesTheUnitWithoutAnHr_ForEveryoneButAdministrators()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var siblingHr = await org.Admin.GetUserAsync(org.SiblingHr.Id);
        (await org.Admin.PostAsync($"/api/v1/users/{org.SiblingHr.Id}/block", ifMatch: siblingHr.ETag)).Expect(HttpStatusCode.OK);

        var forUser = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.OK).Json!;
        var forAdmin = (await org.Admin.GetAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.OK).Json!;
        var ofTeamForUser = (await org.Plain.Client.GetAsync($"/api/v1/career/org-units/{org.Team.Id}/hr")).Expect(HttpStatusCode.OK).Json!;

        forUser["assigned"].Should().BeNull();
        forUser["inherited"].Should().BeNull("the role is not passed further up while the unit's own HR cannot act");
        forAdmin["assigned"]!["isActive"]!.GetValue<bool>().Should().BeFalse();
        forAdmin["inherited"]!["isActive"]!.GetValue<bool>().Should().BeTrue();
        ofTeamForUser["inherited"]!["userId"]!.GetValue<Guid>().Should().Be(org.Hr.Id, "a unit with no assignment of its own is not affected");
        ofTeamForUser["inherited"]!.AsObject().ContainsKey("isActive").Should().BeFalse("only administrators see whether the account can act");
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
            var assign = await member.Client.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { userId = member.Id });
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
        var first = await org.Admin.CreateUserAsync();
        var second = await org.Admin.CreateUserAsync();

        var assigned = (await org.Admin.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { userId = first.Id })).Expect(HttpStatusCode.NoContent);
        var replaced = (await org.Admin.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { userId = second.Id })).Expect(HttpStatusCode.NoContent);
        var removed = (await org.Admin.DeleteAsync($"/api/v1/career/org-units/{org.Team.Id}/hr")).Expect(HttpStatusCode.NoContent);

        var creation = (await org.Admin.AuditOfRequestAsync(assigned.RequestId!)).Single()!;
        var change = (await org.Admin.AuditOfRequestAsync(replaced.RequestId!)).Single()!;
        var deletion = (await org.Admin.AuditOfRequestAsync(removed.RequestId!)).Single()!;
        creation["action"]!.GetValue<string>().Should().Be("OrgUnitHr.Created");
        creation["newValue"]!["userId"]!.GetValue<Guid>().Should().Be(first.Id);
        change["action"]!.GetValue<string>().Should().Be("OrgUnitHr.Updated");
        change["oldValue"]!["userId"]!.GetValue<Guid>().Should().Be(first.Id);
        change["newValue"]!["userId"]!.GetValue<Guid>().Should().Be(second.Id);
        deletion["action"]!.GetValue<string>().Should().Be("OrgUnitHr.Deleted");
        deletion["oldValue"]!["userId"]!.GetValue<Guid>().Should().Be(second.Id);
    }
}
