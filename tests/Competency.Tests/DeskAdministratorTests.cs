using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// What the desk of a global administrator expects of them: the gaps of a unit one by one, invitations that did not work out and the
/// invitation sent again from the desk, the positions and accounts out of order, and that nobody else is told any of it.
/// </summary>
public sealed class DeskAdministratorTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac33_Ac34_Ac147_AUnitWithEmployees_GoesFromWithoutAHeadToConnected_AsEachGapIsClosed()
    {
        var admin = await fixture.Host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);

        var withoutHead = (await admin.ItemsAsync()).ItemFor(unit.Id);
        (await admin.SetHeadAsync(unit, employee.Id)).Expect(HttpStatusCode.OK);
        var withoutAccount = (await admin.ItemsAsync()).ItemFor(unit.Id);
        var invited = await admin.InviteUserAsync(employeeId: employee.Id);
        var withInvited = (await admin.ItemsAsync()).ItemFor(unit.Id);
        (await admin.PostAsync($"/api/v1/users/{invited.Id}/block", ifMatch: (await admin.GetUserAsync(invited.Id)).ETag)).Expect(HttpStatusCode.OK);
        var withBlocked = (await admin.ItemsAsync()).ItemFor(unit.Id);

        withoutHead.Kinds().Should().Equal("UnitWithoutHead", "UnitWithoutHr");
        withoutHead["group"]!.GetValue<string>().Should().Be("UnitsToConnect");
        withoutHead["isReference"]!.GetValue<bool>().Should().BeFalse();
        withoutHead["object"]!["type"]!.GetValue<string>().Should().Be("OrgUnit");
        withoutHead.FirstReason()["occurredAt"].Should().BeNull("a state of the data has no date of an event");
        withoutAccount.Kinds().Should().Equal("HeadWithoutAccount", "UnitWithoutHr");
        withoutAccount.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(employee.Id);
        withInvited.Kinds().Should().Equal("HeadAccountInvited", "UnitWithoutHr");
        withBlocked.Kinds().Should().Equal("HeadAccountBlocked", "UnitWithoutHr");
    }

    [Fact]
    public async Task Ac36_Ac38_Ac39_AnInvitationWhoseLinkExpired_IsAnActionOnTheAccount_SentAgainFromTheDesk_AndTheUnitItBlocksFollowsIt()
    {
        var admin = await fixture.Host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        (await admin.SetHeadAsync(unit, employee.Id)).Expect(HttpStatusCode.OK);
        var invited = await admin.InviteUserAsync(employeeId: employee.Id);
        await fixture.Host.ExecuteAsync($"UPDATE users SET link_expires_at = now() - interval '1 second' WHERE id = '{invited.Id}'");

        var items = await admin.ItemsAsync();

        var account = items.ItemFor(invited.Id);
        account.Kinds().Should().Equal("InvitationNotAccepted");
        account["group"]!.GetValue<string>().Should().Be("Invitations");
        account["isReference"]!.GetValue<bool>().Should().BeFalse();
        account["object"]!["type"]!.GetValue<string>().Should().Be("UserAccount");
        account.FirstReason()["details"]!["linkState"]!.GetValue<string>().Should().Be("Expired");
        account.FirstReason()["occurredAt"].Should().NotBeNull("an invitation is dated by the expiry of its link");
        account["invitation"]!["accountId"]!.GetValue<Guid>().Should().Be(invited.Id);
        items.ItemFor(unit.Id).Kinds().Should().Equal("UnitWithoutHr");

        var version = account["invitation"]!["version"]!.GetValue<int>();
        (await admin.PostAsync($"/api/v1/users/{invited.Id}/resend-invitation", ifMatch: $"\"{version}\"")).Expect(HttpStatusCode.OK);

        var after = await admin.ItemsAsync();
        after.ItemOf(invited.Id).Should().BeNull("the new link works for a week");
        after.ItemFor(unit.Id).Kinds().Should().Equal("HeadAccountInvited", "UnitWithoutHr");
    }

    [Theory]
    [InlineData("expired", "Expired")]
    [InlineData("soon", "ExpiringSoon")]
    [InlineData("revoked", "Revoked")]
    [InlineData("far", null)]
    public async Task Ac36_Ac37_AnInvitationNeedsTheDesk_OnlyWhenItsLinkNoLongerWorksOrSoonWillNot(string state, string? linkState)
    {
        var admin = await fixture.Host.AdminAsync();
        var invited = await InvitedAccountOfAnEmployeeAsync(admin);
        var setting = state switch
        {
            "expired" => "link_expires_at = now() - interval '1 hour'",
            "soon" => "link_expires_at = now() + interval '47 hours'",
            "revoked" => "link_security_stamp = 'someone-else'",
            _ => "link_expires_at = now() + interval '49 hours'",
        };
        await fixture.Host.ExecuteAsync($"UPDATE users SET {setting} WHERE id = '{invited.Id}'");

        var item = (await admin.ItemsAsync()).ItemOf(invited.Id);

        if (linkState is null)
        {
            item.Should().BeNull();
            return;
        }

        item.Should().NotBeNull();
        item!.FirstReason()["details"]!["linkState"]!.GetValue<string>().Should().Be(linkState);
        (item.FirstReason()["occurredAt"] is null).Should().Be(linkState == "Revoked", "a voided link stopped working before its expiry, which is not worth showing");
    }

    [Fact]
    public async Task Ac36_ABlockedInvitedAccount_NeedsNoInvitationFromTheDesk()
    {
        var admin = await fixture.Host.AdminAsync();
        var invited = await InvitedAccountOfAnEmployeeAsync(admin);
        await fixture.Host.ExecuteAsync($"UPDATE users SET link_expires_at = now() - interval '1 hour' WHERE id = '{invited.Id}'");
        (await admin.PostAsync($"/api/v1/users/{invited.Id}/block", ifMatch: (await admin.GetUserAsync(invited.Id)).ETag)).Expect(HttpStatusCode.OK);

        (await admin.ItemsAsync()).ItemOf(invited.Id).Should().BeNull();
    }

    [Fact]
    public async Task Ac35_AUnitWhoseHrCannotAct_IsWithoutAnHr_NamingTheAssigneeAndWhyTheirAccountDoesNotAct()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        (await org.Admin.ItemsAsync()).ItemOf(org.Sibling.Id)?.Kinds().Should().NotContain("UnitWithoutHr");

        (await org.Admin.PostAsync($"/api/v1/users/{org.SiblingHr.Id}/block", ifMatch: (await org.Admin.GetUserAsync(org.SiblingHr.Id)).ETag)).Expect(HttpStatusCode.OK);

        var item = (await org.Admin.ItemsAsync()).ItemFor(org.Sibling.Id);
        item.Kinds().Should().Equal("UnitWithoutHr");
        item["isReference"]!.GetValue<bool>().Should().BeFalse();
        item.FirstReason()["details"]!["accountState"]!.GetValue<string>().Should().Be("Blocked");
        item.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.SiblingHr.Employee);
    }

    [Fact]
    public async Task Ac143_Ac142_AUnitWithEmployeesAndNoPositions_IsAnActionWithTheirNumber_AndEachEmployeeNamesTheirPositionAndUnit()
    {
        var admin = await fixture.Host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id, position: Scenarios.Unique("Должность"));
        var positionId = (await admin.GetEmployeeAsync(employee.Id)).Json!["positionId"]!.GetValue<Guid>();
        await fixture.Host.ExecuteAsync($"DELETE FROM org_unit_positions WHERE org_unit_id = '{unit.Id}'");

        var items = await admin.ItemsAsync();

        var unitItem = items.ItemFor(unit.Id);
        unitItem.Kinds().Should().Contain("UnitWithoutPositions");
        unitItem.ReasonOf("UnitWithoutPositions")!["details"]!["employeeCount"]!.GetValue<int>().Should().Be(1);
        var employeeItem = items.ItemFor(employee.Id);
        employeeItem.Kinds().Should().Equal("PositionOutsideList");
        employeeItem["group"]!.GetValue<string>().Should().Be("OrgCleanup");
        employeeItem["isReference"]!.GetValue<bool>().Should().BeFalse();
        employeeItem.FirstReason()["details"]!["entries"]!.AsArray().Select(entry => entry!["id"]!.GetValue<Guid>()).Should().Equal(positionId, unit.Id);

        (await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions", new { positionId })).Expect(HttpStatusCode.NoContent);

        var after = await admin.ItemsAsync();
        after.ItemOf(employee.Id).Should().BeNull();
        (after.ItemOf(unit.Id)?.Kinds() ?? []).Should().NotContain("UnitWithoutPositions");
    }

    [Fact]
    public async Task Ac144_Ac145_AnInactivePositionHeldByEmployeesAndAnUnusedOne_AreForReference_AndRestoringTheFirstClearsIt()
    {
        var admin = await fixture.Host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id, position: Scenarios.Unique("Должность"));
        var held = (await admin.GetEmployeeAsync(employee.Id)).Json!["positionId"]!.GetValue<Guid>();
        var unused = (await admin.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);
        var current = (await admin.GetAsync($"/api/v1/positions/{held}")).Expect(HttpStatusCode.OK);
        var deactivated = (await admin.PostAsync($"/api/v1/positions/{held}/deactivate", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);

        var items = await admin.ItemsAsync();

        var inactive = items.ItemFor(held);
        inactive.Kinds().Should().Equal("InactivePositionInUse");
        inactive["isReference"]!.GetValue<bool>().Should().BeTrue();
        inactive["group"]!.GetValue<string>().Should().Be("InactivePositions");
        inactive.FirstReason()["details"]!["employeeCount"]!.GetValue<int>().Should().Be(1);
        var notUsed = items.ItemFor(unused.Id);
        notUsed.Kinds().Should().Equal("UnusedPosition");
        notUsed["isReference"]!.GetValue<bool>().Should().BeTrue();
        notUsed["group"]!.GetValue<string>().Should().Be("UnusedPositions");

        (await admin.PostAsync($"/api/v1/positions/{held}/activate", ifMatch: deactivated.ETag)).Expect(HttpStatusCode.OK);
        (await admin.PostAsync($"/api/v1/org-units/{unit.Id}/positions", new { positionId = unused.Id })).Expect(HttpStatusCode.NoContent);

        var after = await admin.ItemsAsync();
        after.ItemOf(held).Should().BeNull("the position is back in work");
        after.ItemOf(unused.Id).Should().BeNull("a unit lists the position now");
    }

    [Fact]
    public async Task Ac148_AnEmptyUnit_IsForReference_AndHidesItsOtherGaps_UntilAnEmployeeJoins()
    {
        var admin = await fixture.Host.AdminAsync();
        var unit = await admin.CreateUnitAsync();

        var parent = await admin.CreateUnitAsync();
        var child = await admin.CreateUnitAsync(parent.Id);
        var empty = (await admin.ItemsAsync()).ItemFor(unit.Id);
        await admin.CreateEmployeeAsync(unit.Id);
        await admin.CreateEmployeeAsync(child.Id);
        var occupied = (await admin.ItemsAsync()).ItemFor(unit.Id);
        var withActiveChild = (await admin.ItemsAsync()).ItemFor(parent.Id);

        empty.Kinds().Should().Equal("EmptyUnit");
        empty["isReference"]!.GetValue<bool>().Should().BeTrue();
        empty["group"]!.GetValue<string>().Should().Be("EmptyUnits");
        occupied.Kinds().Should().Equal("UnitWithoutHead", "UnitWithoutHr");
        occupied["isReference"]!.GetValue<bool>().Should().BeFalse();
        withActiveChild.Kinds().Should().Equal("UnitWithoutHead", "UnitWithoutHr");
    }

    [Fact]
    public async Task Ac146_AnAccountWithoutAnEmployee_IsAnAction_ExceptOneWithOnlyTheAdministratorRole_OrABlockedOne()
    {
        var admin = await fixture.Host.AdminAsync();
        var plain = await admin.CreateUserAsync();
        var hr = await admin.CreateUserAsync(Scenarios.Hr);
        var onlyAdministrator = await admin.CreateUserAsync(Scenarios.GlobalAdmin);
        var administratorAndHr = (await admin.PostAsync(
            "/api/v1/users",
            new { email = $"{Scenarios.Unique("both")}@test.local", roles = new[] { Scenarios.Hr, Scenarios.GlobalAdmin }, employeeId = (Guid?)null })).Expect(HttpStatusCode.Created);
        var blocked = await admin.CreateUserAsync();
        (await admin.PostAsync($"/api/v1/users/{blocked.Id}/block", ifMatch: (await admin.GetUserAsync(blocked.Id)).ETag)).Expect(HttpStatusCode.OK);

        var items = await admin.ItemsAsync();

        items.ItemFor(plain.Id).Kinds().Should().Equal("AccountWithoutEmployee");
        items.ItemFor(plain.Id)["group"]!.GetValue<string>().Should().Be("OrgCleanup");
        items.ItemFor(plain.Id)["isReference"]!.GetValue<bool>().Should().BeFalse();
        items.ItemFor(plain.Id).FirstReason()["details"].Should().BeNull();
        items.ItemFor(hr.Id).FirstReason()["details"]!["hasHrRole"]!.GetValue<bool>().Should().BeTrue("an HR needs an employee to be assigned as a unit's HR");
        items.ItemFor(administratorAndHr.Id).Kinds().Should().Equal("AccountWithoutEmployee");
        items.ItemOf(onlyAdministrator.Id).Should().BeNull("an administrator account is meant to stay unbound");
        items.ItemOf(blocked.Id).Should().BeNull();
    }

    [Fact]
    public async Task Ac118_Ac150_TheseChecksAreTheAdministratorsAlone_AndTheyAreNotShownTheChecksOfHeadsAndHr()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var admin = org.Admin;
        var stray = await admin.CreateUserAsync();
        var emptyUnit = await admin.CreateUnitAsync();
        var track = await admin.MakeTrackAsync(org.Team.Id, (await admin.MakeDirectionAsync()).Id);

        var items = await admin.ItemsAsync();

        items.ItemOf(track.Id)!.Kinds().Should().Equal("DraftAwaitsApproval");
        items.SelectMany(item => item.Kinds()).Should().NotContain(
            ["TrackWithoutLevels", "TrackWithoutPositions", "LevelWithoutPosition", "PositionWithoutTrack", "VersionNotSubmitted", "UnitWithoutMatrix"]);
        foreach (var member in new[] { org.TeamHead, org.Hr, org.Plain })
        {
            var theirs = await member.Client.ItemsAsync();
            theirs.ItemOf(stray.Id).Should().BeNull();
            theirs.ItemOf(emptyUnit.Id).Should().BeNull();
            theirs.SelectMany(item => item.Kinds()).Should().NotContain(["AccountWithoutEmployee", "InvitationNotAccepted", "PositionOutsideList", "UnitWithoutHead", "UnitWithoutHr", "EmptyUnit"]);
        }
    }

    private static async Task<TestUser> InvitedAccountOfAnEmployeeAsync(ApiClient admin)
    {
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id, position: Scenarios.Unique("Должность"));
        return await admin.InviteUserAsync(employeeId: employee.Id);
    }
}
