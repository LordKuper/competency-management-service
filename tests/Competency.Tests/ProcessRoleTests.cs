using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The career process roles are worked out from the org structure and the HR assignments each time they are asked, so a change of head or
/// assignment applies at once: a unit head is the account bound to the working employee the unit names as head, and the responsible HR of a
/// unit is the one assigned to it or to the nearest unit above.
/// </summary>
public sealed class ProcessRoleTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac7_Me_ListsTheUnitsTheAccountHeadsAndTheUnitsItAnswersForAsHr()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var head = await MeAsync(org.TeamHead);
        var rootHr = await MeAsync(org.Hr);
        var siblingHr = await MeAsync(org.SiblingHr);
        var plain = await MeAsync(org.Plain);

        head.Headed.Should().Equal(org.Team.Id);
        head.Hr.Should().BeEmpty();
        rootHr.Headed.Should().BeEmpty();
        rootHr.Hr.Should().BeEquivalentTo([org.Root.Id, org.Department.Id, org.Team.Id], "the assignment reaches the units below down to the unit that has an HR of its own");
        siblingHr.Hr.Should().Equal(org.Sibling.Id);
        plain.Headed.Should().BeEmpty();
        plain.Hr.Should().BeEmpty();
    }

    [Fact]
    public async Task Ac7_Me_NamesTheUnitsInNameOrder()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var rootHr = (await org.Hr.Client.GetAsync("/api/v1/career/me")).Expect(HttpStatusCode.OK).Json!["hrUnits"]!.AsArray();

        rootHr.Select(unit => unit!["name"]!.GetValue<string>()).Should().BeInAscendingOrder();
        rootHr.Select(unit => unit!["name"]!.GetValue<string>()).Should().BeEquivalentTo(
            [org.Root.Json!["name"]!.GetValue<string>(), org.Department.Json!["name"]!.GetValue<string>(), org.Team.Json!["name"]!.GetValue<string>()]);
    }

    [Fact]
    public async Task Ac7_ChangingTheHeadInTheOrgStructure_ChangesTheRolesAtOnce()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var employee = await org.Admin.CreateEmployeeAsync(org.Team.Id);
        var account = await org.Admin.CreateUserAsync(employeeId: employee.Id);
        var next = await account.SignInAsync(fixture.Host);

        (await org.Admin.SetHeadAsync(org.Team, employee.Id)).Expect(HttpStatusCode.OK);

        (await MeAsync(new Member(account, next))).Headed.Should().Equal(org.Team.Id);
        (await MeAsync(org.TeamHead)).Headed.Should().BeEmpty("the former head has lost the role without any other action");
    }

    [Fact]
    public async Task Ac7_AnAssignmentRemovedOrMovedInTheOrgStructure_ChangesTheHrRolesAtOnce()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        (await org.Admin.DeleteAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.NoContent);

        (await MeAsync(org.SiblingHr)).Hr.Should().BeEmpty();
        (await MeAsync(org.Hr)).Hr.Should().BeEquivalentTo([org.Root.Id, org.Department.Id, org.Team.Id, org.Sibling.Id]);
    }

    [Fact]
    public async Task Ac7_ADeactivatedUnit_TakesPartInNoRole()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var unit = await org.Admin.CreateUnitAsync(org.Root.Id);
        var other = await org.Admin.CreateUserAsync();
        var session = await other.SignInAsync(fixture.Host);
        (await org.Admin.PutAsync($"/api/v1/career/org-units/{unit.Id}/hr", new { userId = other.Id })).Expect(HttpStatusCode.NoContent);
        (await MeAsync(new Member(other, session))).Hr.Should().Equal(unit.Id);

        (await org.Admin.PostAsync($"/api/v1/org-units/{unit.Id}/deactivate", ifMatch: unit.ETag)).Expect(HttpStatusCode.OK);

        (await MeAsync(new Member(other, session))).Hr.Should().BeEmpty();
    }

    [Fact]
    public async Task Ac7_Me_RefusesASignedOutCaller()
    {
        (await fixture.Host.Anonymous().GetAsync("/api/v1/career/me")).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<(List<Guid> Headed, List<Guid> Hr)> MeAsync(Member member)
    {
        var me = (await member.Client.GetAsync("/api/v1/career/me")).Expect(HttpStatusCode.OK).Json!;
        return (Ids(me["headedUnits"]), Ids(me["hrUnits"]));

        static List<Guid> Ids(JsonNode? units) => [.. units!.AsArray().Select(unit => unit!["id"]!.GetValue<Guid>())];
    }
}
