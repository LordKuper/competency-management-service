using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The matrices of a unit without a head are led by the head of the nearest unit above that has one: the unit is among that head's matrix
/// units, the head creates and hands on its matrices, and the superior's step is skipped because the leader is the superior. A head named
/// for the unit takes the matrices over at once.
/// </summary>
public sealed class MatrixHeadlessUnitTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac90_AHeadLeadsTheMatricesOfTheUnitsBelowItWithoutAHead_UpToTheNextUnitThatHasOne()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var belowTeam = await org.Admin.CreateUnitAsync(org.Team.Id);
        var belowHeadless = await org.Admin.CreateUnitAsync(belowTeam.Id);
        var belowDepartment = await org.Admin.CreateUnitAsync(org.Department.Id);

        var team = await MatrixUnitsAsync(org.TeamHead);
        var department = await MatrixUnitsAsync(org.DepartmentHead);

        team.Should().BeEquivalentTo([org.Team.Id, belowTeam.Id, belowHeadless.Id]);
        department.Should().Contain([org.Department.Id, belowDepartment.Id]).And.NotContain([belowTeam.Id, belowHeadless.Id], "a unit with a head between leads those itself");
        (await HeadedUnitsAsync(org.TeamHead)).Should().Equal(org.Team.Id);
    }

    [Fact]
    public async Task Ac90_TheHeadAbove_CreatesAndHandsOnTheMatrixOfAHeadlessUnit_WithoutTheSuperiorStep()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var headless = await org.Admin.CreateUnitAsync(org.Team.Id);
        var kit = await (await MatrixKit.CreateAsync(org, headless.Id)).PrepareAsync();

        var submitted = (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var published = (await kit.TransitAsync(org.Hr.Client, "approve")).Expect(HttpStatusCode.OK);

        submitted.Json!["step"]!.GetValue<string>().Should().Be("Hr");
        submitted.Json["superiorApprover"].Should().BeNull();
        published.Json!["status"]!.GetValue<string>().Should().Be("Published");
        (await kit.DecisionsAsync()).Should().Contain("Skipped@Superior").And.EndWith("Published");
    }

    [Fact]
    public async Task Ac90_AHeadWhoIsNotTheLeaderOfAUnit_CannotCreateItsMatrix()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var headless = await org.Admin.CreateUnitAsync(org.Team.Id);
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeActiveTrackAsync(headless.Id, direction.Id);

        foreach (var outsider in new[] { org.DepartmentHead, org.SiblingHead, org.Plain })
        {
            (await outsider.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = headless.Id, directionId = direction.Id, trackId = track.Id }))
                .Status.Should().Be(HttpStatusCode.Forbidden, outsider.User.Email);
        }
    }

    [Fact]
    public async Task Ac90_WhenTheUnitGetsAHead_ItsMatricesPassToHim_AndTheSuperiorStepReturns()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var unit = await org.Admin.CreateUnitAsync(org.Team.Id);
        var before = await MatrixUnitsAsync(org.TeamHead);

        var head = await org.Admin.HeadOfAsync(fixture.Host, unit);
        var kit = await (await MatrixKit.CreateAsync(org, unit.Id, author: head.Client)).PrepareAsync();
        var submitted = (await kit.TransitAsync(head.Client, "submit")).Expect(HttpStatusCode.OK);

        before.Should().Contain(unit.Id);
        (await MatrixUnitsAsync(org.TeamHead)).Should().NotContain(unit.Id);
        (await MatrixUnitsAsync(head)).Should().Equal(unit.Id);
        submitted.Json!["step"]!.GetValue<string>().Should().Be("Superior");
        submitted.Json["superiorApprover"]!["userId"]!.GetValue<Guid>().Should().Be(org.TeamHead.Id);
    }

    private static async Task<List<Guid>> MatrixUnitsAsync(Member member) => await UnitsAsync(member, "matrixUnits");

    private static async Task<List<Guid>> HeadedUnitsAsync(Member member) => await UnitsAsync(member, "headedUnits");

    private static async Task<List<Guid>> UnitsAsync(Member member, string key)
    {
        JsonNode me = (await member.Client.GetAsync("/api/v1/career/me")).Expect(HttpStatusCode.OK).Json!;
        return [.. me[key]!.AsArray().Select(unit => unit!["id"]!.GetValue<Guid>())];
    }
}
