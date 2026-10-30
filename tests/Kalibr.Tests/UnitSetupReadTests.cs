using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The connect-a-unit screen's read for a unit under others: the ancestors from the root, the inherited HR and positions; and that the
/// read is the administrator's alone.
/// </summary>
public sealed class UnitSetupReadTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac81_ATeamUnder_ShowsItsAncestorsFromTheRoot_TheInheritedHrAndThePositionsItCannotTakeFromItsOwnList()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var child = await org.Admin.CreateUnitAsync(org.Team.Id);
        var inherited = await org.Admin.PositionAsync(org.Root.Id, Scenarios.Unique("Должность"));

        var setup = (await org.Admin.GetAsync($"/api/v1/career/org-units/{child.Id}/setup")).Expect(HttpStatusCode.OK).Json!;

        setup["unit"]!.AsArray().Select(unit => unit!["id"]!.GetValue<Guid>()).Should().Equal(org.Root.Id, org.Department.Id, org.Team.Id, child.Id);
        setup["hr"]!["employeeId"]!.GetValue<Guid>().Should().Be(org.Hr.Employee);
        setup["hr"]!["orgUnitId"]!.GetValue<Guid>().Should().Be(org.Root.Id, "the HR is the one assigned to the nearest unit above");
        setup["positions"]!["own"]!.AsArray().Should().BeEmpty();
        setup["positions"]!["inherited"]!.AsArray().Select(position => position!["id"]!.GetValue<Guid>()).Should().Contain(inherited);
        Rows(setup).Should().Contain(["Hr/Ready/", "Positions/Ready/"]);
    }

    [Fact]
    public async Task Ac80_TheSetupOfAUnit_IsTheAdministratorsAlone_AndAnUnknownUnitIsNotFound()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var path = $"/api/v1/career/org-units/{org.Team.Id}/setup";

        foreach (var refused in new[] { org.TeamHead.Client, org.Hr.Client, org.Plain.Client })
        {
            (await refused.GetAsync(path)).Status.Should().Be(HttpStatusCode.Forbidden);
        }

        (await fixture.Host.Anonymous().GetAsync(path)).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await org.Admin.GetAsync($"/api/v1/career/org-units/{Guid.NewGuid()}/setup")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    private static string[] Rows(JsonNode setup) =>
        [.. setup["rows"]!.AsArray().Select(row => $"{row!["row"]}/{row["status"]}/{row["reason"]}")];
}
