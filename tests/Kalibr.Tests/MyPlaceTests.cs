using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// What an employee reads about their place in the company: the unit, the head, the HR, the tracks their position stands for with the
/// level they are on and the levels above with the requirements of the published matrix of their own unit, and nothing unpublished.
/// </summary>
public sealed class MyPlaceTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string Route = "/api/v1/career/me/place";

    [Fact]
    public async Task Ac91_Ac96_Ac97_Ac98_AnEmployeeSeesTheirUnitHeadHrTheirTrackAndLevelAndTheRequirementsOfTheLevelsAbove()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var junior = await EmployeeOnAsync(org, kit, kit.Junior.Id, org.Team.Id);
        var draftTrack = await org.Admin.MakeTrackAsync(org.Team.Id, kit.Direction.Id);
        var draftLevel = await org.Admin.MakeLevelAsync(draftTrack.Id);
        var juniorPosition = (await org.Admin.GetEmployeeAsync(junior.EmployeeId)).Json!["positionId"]!.GetValue<Guid>();
        (await org.Admin.PostAsync($"/api/v1/career/levels/{draftLevel.Id}/positions", new { positionId = juniorPosition }, draftLevel.ETag)).Expect(HttpStatusCode.OK);

        var place = (await junior.Client.GetAsync(Route)).Expect(HttpStatusCode.OK).Json!;

        place["employee"]!["id"]!.GetValue<Guid>().Should().Be(junior.EmployeeId);
        place["unit"]!.AsArray().Select(unit => unit!["id"]!.GetValue<Guid>()).Should().Equal(org.Root.Id, org.Department.Id, org.Team.Id);
        place["head"]!["id"]!.GetValue<Guid>().Should().Be(org.TeamHead.Employee);
        place["responsibleHr"]!["employeeId"]!.GetValue<Guid>().Should().Be(org.Hr.Employee);
        place["noTrack"]!.GetValue<bool>().Should().BeFalse();
        place["positionMatrix"].Should().BeNull();
        var track = place["tracks"]!.AsArray().Single()!;
        track["track"]!["id"]!.GetValue<Guid>().Should().Be(kit.Track.Id, "a draft track the position is linked to stands for nothing yet");
        track["currentLevel"]!["id"]!.GetValue<Guid>().Should().Be(kit.Junior.Id);
        track["matrix"]!["matrixId"]!.GetValue<Guid>().Should().Be(kit.MatrixId);
        track["matrix"]!["number"]!.GetValue<int>().Should().Be(1);
        var next = track["nextLevels"]!.AsArray().Single()!;
        next["id"]!.GetValue<Guid>().Should().Be(kit.Senior.Id);
        next["requirements"]!.AsArray().Select(requirement => (requirement!["competencyId"]!.GetValue<Guid>(), requirement["requiredScore"]!.GetValue<int>(), requirement["type"]!.GetValue<string>()))
            .Should().Equal((kit.Critical.Id, 3, "Critical"), (kit.Profile.Id, 2, "Additional"));
        var scale = (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK).Json!;
        next["requirements"]![0]!["scoreLabel"]!.GetValue<string>().Should().Be(
            scale["points"]!.AsArray().Single(point => point!["score"]!.GetValue<int>() == 3)!["label"]!.GetValue<string>());
        place["publishedMatrices"]!.AsArray().Select(matrix => matrix!["matrixId"]!.GetValue<Guid>()).Should().Equal(kit.MatrixId);
    }

    [Fact]
    public async Task Ac99_Ac92_TheRequirementsComeFromTheMatrixOfTheEmployeesOwnUnit_AndTheLevelAboveTheLastIsNone()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var inSibling = await EmployeeOnAsync(org, kit, kit.Junior.Id, org.Sibling.Id);
        var senior = await EmployeeOnAsync(org, kit, kit.Senior.Id, org.Team.Id);

        var sibling = (await inSibling.Client.GetAsync(Route)).Expect(HttpStatusCode.OK).Json!;
        var top = (await senior.Client.GetAsync(Route)).Expect(HttpStatusCode.OK).Json!;

        var track = sibling["tracks"]!.AsArray().Single()!;
        track["currentLevel"]!["id"]!.GetValue<Guid>().Should().Be(kit.Junior.Id);
        track["matrix"].Should().BeNull("the sibling unit has no published matrix on the track");
        track["nextLevels"]!.AsArray().Single()!["requirements"].Should().BeNull("levels are listed without requirements then");
        sibling["publishedMatrices"]!.AsArray().Should().BeEmpty("a matrix of another unit is not shown");
        top["tracks"]!.AsArray().Single()!["nextLevels"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Ac92_Ac117_ANewVersionInWork_IsNeverShown_UntilItIsPublished()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var junior = await EmployeeOnAsync(org, kit, kit.Junior.Id, org.Team.Id);
        var draft = (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created);
        (await kit.SaveAsync(org.TeamHead.Client, kit.Content(seniorCriticalScore: 4), versionId: draft.Id)).Expect(HttpStatusCode.OK);

        var before = (await junior.Client.GetAsync(Route)).Expect(HttpStatusCode.OK).Json!;
        (await kit.TransitAsync(org.TeamHead.Client, "submit", versionId: draft.Id)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve", versionId: draft.Id)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "approve", versionId: draft.Id)).Expect(HttpStatusCode.OK);
        var after = (await junior.Client.GetAsync(Route)).Expect(HttpStatusCode.OK).Json!;

        Score(before).Should().Be(3);
        before["tracks"]![0]!["matrix"]!["number"]!.GetValue<int>().Should().Be(1);
        Score(after).Should().Be(4);
        after["tracks"]![0]!["matrix"]!["number"]!.GetValue<int>().Should().Be(2);
    }

    [Fact]
    public async Task Ac95_Ac100_AnAccountBoundToNoEmployee_AndAnEmployeeWhosePositionHasNoTrack_GetAnExplainableAnswer()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var unbound = await org.Plain.Client.GetAsync(Route);
        var unlinked = await EmployeeAsync(org, org.Team.Id, Scenarios.Unique("Должность"));

        var plainPlace = unbound.Expect(HttpStatusCode.OK).Json!;
        var noTrack = (await unlinked.Client.GetAsync(Route)).Expect(HttpStatusCode.OK).Json!;

        plainPlace["employee"].Should().BeNull();
        plainPlace["unit"]!.AsArray().Should().BeEmpty();
        plainPlace["tracks"]!.AsArray().Should().BeEmpty();
        plainPlace["noTrack"]!.GetValue<bool>().Should().BeFalse("an unbound account has no position to lack a track for");
        noTrack["employee"].Should().NotBeNull();
        noTrack["tracks"]!.AsArray().Should().BeEmpty();
        noTrack["noTrack"]!.GetValue<bool>().Should().BeTrue();
        noTrack["publishedMatrices"]!.AsArray().Select(matrix => matrix!["matrixId"]!.GetValue<Guid>()).Should().Equal(kit.MatrixId);
    }

    [Fact]
    public async Task Ac115_TheMethodAnswersAboutTheSignedInUserAlone_ToASignedInCallerOnly()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var other = await org.TeamHead.Client.GetAsync($"{Route}?userId={org.Hr.Id}&employeeId={org.Hr.Employee}");

        (await fixture.Host.Anonymous().GetAsync(Route)).Status.Should().Be(HttpStatusCode.Unauthorized);
        other.Expect(HttpStatusCode.OK).Json!["employee"]!["id"]!.GetValue<Guid>().Should().Be(org.TeamHead.Employee);
    }

    private static int Score(JsonNode place) =>
        place["tracks"]![0]!["nextLevels"]![0]!["requirements"]!.AsArray().First(requirement => requirement!["type"]!.GetValue<string>() == "Critical")!["requiredScore"]!.GetValue<int>();

    private static async Task<MatrixKit> PublishedAsync(CareerOrg org)
    {
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        return kit;
    }

    private async Task<(ApiClient Client, Guid EmployeeId)> EmployeeOnAsync(CareerOrg org, MatrixKit kit, Guid levelId, Guid unitId)
    {
        var positions = (await org.Admin.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray()
            .Single(level => level!["id"]!.GetValue<Guid>() == levelId)!["positions"]!.AsArray();
        var name = positions[0]!["name"]!.GetValue<string>();
        return await EmployeeAsync(org, unitId, name);
    }

    private async Task<(ApiClient Client, Guid EmployeeId)> EmployeeAsync(CareerOrg org, Guid unitId, string positionName)
    {
        var employee = await org.Admin.CreateEmployeeAsync(unitId, position: positionName);
        return (await (await org.Admin.CreateUserAsync(employeeId: employee.Id)).SignInAsync(fixture.Host), employee.Id);
    }
}
