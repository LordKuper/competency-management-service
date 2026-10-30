using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The matrix of a position, kept by a unit with no track: who may create one and for which positions, its one column, the route to its
/// publication, and what the desk and the employee's place say about the position once it has a matrix.
/// </summary>
public sealed class PositionMatrixTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string BasisMessage = "Укажите либо область и трек, либо должность";
    private const string DuplicateMessage = "У подразделения уже есть матрица для этой должности";
    private const string UnavailableMessage = "Должность неактивна или недоступна подразделению матрицы";
    private const string ColumnMessage = "У матрицы должности один столбец требований — её должность";

    [Fact]
    public async Task Ac121_Ac122_APositionMatrixIsTheUnitHeadsToCreate_ForAnActivePositionTheUnitOrAUnitAboveListsAndOncePerUnit()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var own = await PositionAsync(org, org.Team.Id);
        var above = await PositionAsync(org, org.Root.Id);
        var inSibling = await PositionAsync(org, org.Sibling.Id);
        var inactive = await PositionAsync(org, org.Team.Id);
        var current = (await org.Admin.GetAsync($"/api/v1/positions/{inactive}")).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/positions/{inactive}/deactivate", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
        var direction = await org.Admin.MakeDirectionAsync();

        var created = await CreateAsync(org.TeamHead.Client, org.Team.Id, own);
        var inherited = await CreateAsync(org.TeamHead.Client, org.Team.Id, above);
        var twice = await CreateAsync(org.TeamHead.Client, org.Team.Id, own);
        var beside = await CreateAsync(org.TeamHead.Client, org.Team.Id, inSibling);
        var notActive = await CreateAsync(org.TeamHead.Client, org.Team.Id, inactive);
        var byHr = await CreateAsync(org.Hr.Client, org.Team.Id, await PositionAsync(org, org.Team.Id));
        var byOtherHead = await CreateAsync(org.SiblingHead.Client, org.Team.Id, await PositionAsync(org, org.Team.Id));
        var both = await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id, positionId = own, directionId = direction.Id });
        var neither = await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id });

        created.Expect(HttpStatusCode.Created).Json!["positionId"]!.GetValue<Guid>().Should().Be(own);
        created.Json!["trackId"].Should().BeNull();
        var firstVersion = (await org.Admin.GetAsync($"/api/v1/career/matrix-versions/{created.Json["inWorkVersion"]!["id"]!.GetValue<Guid>()}")).Expect(HttpStatusCode.OK).Json!;
        firstVersion["content"]!["levels"]!.AsArray().Select(level => level!["levelId"]!.GetValue<Guid>()).Should().Equal(own);
        inherited.Status.Should().Be(HttpStatusCode.Created);
        twice.Status.Should().Be(HttpStatusCode.Conflict);
        twice.FieldErrors("positionId").Should().Equal(DuplicateMessage);
        beside.FieldErrors("positionId").Should().Equal(UnavailableMessage);
        notActive.FieldErrors("positionId").Should().Equal(UnavailableMessage);
        byHr.Status.Should().Be(HttpStatusCode.Forbidden);
        byOtherHead.Status.Should().Be(HttpStatusCode.Forbidden);
        both.FieldErrors("positionId").Should().Equal(BasisMessage);
        neither.FieldErrors("positionId").Should().Equal(BasisMessage);
    }

    [Fact]
    public async Task Ac123_Ac125_TheOneColumnVersion_IsCheckedByItsPositionAndItsCoefficients_AndHandedOnLikeAnyVersion()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var position = await PositionAsync(org, org.Team.Id);
        var matrix = (await CreateAsync(org.TeamHead.Client, org.Team.Id, position)).Expect(HttpStatusCode.Created);
        var versionId = matrix.Json!["inWorkVersion"]!["id"]!.GetValue<Guid>();
        var critical = await org.Admin.MakeCompetencyAsync();
        var profile = await org.Admin.MakeCompetencyAsync();

        var foreign = await SaveAsync(org, versionId, Content(Guid.NewGuid(), critical.Id, profile.Id));
        var empty = await ProblemsAsync(org, versionId);
        (await SaveAsync(org, versionId, Content(position, critical.Id, profile.Id, weight: 90))).Expect(HttpStatusCode.OK);
        var badWeight = await ProblemsAsync(org, versionId);
        (await SaveAsync(org, versionId, Content(position, critical.Id, profile.Id))).Expect(HttpStatusCode.OK);
        var complete = await ProblemsAsync(org, versionId);

        foreign.Status.Should().Be(HttpStatusCode.BadRequest);
        foreign.FieldErrors("content").Should().Equal(ColumnMessage);
        empty.Should().Contain("NoCompetencies");
        badWeight.Should().Equal("BlockWeights");
        complete.Should().BeEmpty();
        (await Transit(org.TeamHead.Client, versionId, "submit")).Expect(HttpStatusCode.OK);
        (await Transit(org.DepartmentHead.Client, versionId, "approve")).Expect(HttpStatusCode.OK);
        (await Transit(org.Hr.Client, versionId, "approve")).Expect(HttpStatusCode.OK);
        var published = (await org.Plain.Client.GetAsync($"/api/v1/career/matrix-versions/{versionId}")).Expect(HttpStatusCode.OK).Json!;
        published["status"]!.GetValue<string>().Should().Be("Published");
    }

    [Fact]
    public async Task Ac196_Ac197_APositionWithAMatrixIsNotWithoutATrack_OnTheDeskAndInTheEmployeesPlace_ByTheStatusOfItsVersion()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var name = Scenarios.Unique("Должность");
        var employee = await org.Admin.CreateEmployeeAsync(org.Team.Id, position: name);
        var positionId = (await org.Admin.GetEmployeeAsync(employee.Id)).Json!["positionId"]!.GetValue<Guid>();
        var client = await (await org.Admin.CreateUserAsync(employeeId: employee.Id)).SignInAsync(fixture.Host);
        var critical = await org.Admin.MakeCompetencyAsync();
        var profile = await org.Admin.MakeCompetencyAsync();

        var withoutMatrix = await org.TeamHead.Client.ItemsAsync();
        var matrix = (await CreateAsync(org.TeamHead.Client, org.Team.Id, positionId)).Expect(HttpStatusCode.Created);
        var versionId = matrix.Json!["inWorkVersion"]!["id"]!.GetValue<Guid>();
        var draftHead = await org.TeamHead.Client.ItemsAsync();
        var draftEmployee = await client.ItemsAsync();
        (await SaveAsync(org, versionId, Content(positionId, critical.Id, profile.Id))).Expect(HttpStatusCode.OK);
        (await Transit(org.TeamHead.Client, versionId, "submit")).Expect(HttpStatusCode.OK);
        (await Transit(org.DepartmentHead.Client, versionId, "approve")).Expect(HttpStatusCode.OK);
        (await Transit(org.Hr.Client, versionId, "approve")).Expect(HttpStatusCode.OK);
        var publishedEmployee = await client.ItemsAsync();
        var place = (await client.GetAsync("/api/v1/career/me/place")).Expect(HttpStatusCode.OK).Json!;

        withoutMatrix.ItemFor(positionId).Kinds().Should().Equal("PositionWithoutTrack");
        draftHead.ItemOf(positionId).Should().BeNull("a position with a matrix of any unit in any status is not one without a track");
        draftEmployee.ItemFor(employee.Id).Kinds().Should().Equal("NoTrackForPosition");
        publishedEmployee.ItemOf(employee.Id).Should().BeNull();
        place["noTrack"]!.GetValue<bool>().Should().BeFalse();
        place["tracks"]!.AsArray().Should().BeEmpty();
        var requirements = place["positionMatrix"]!["requirements"]!.AsArray();
        place["positionMatrix"]!["matrixId"]!.GetValue<Guid>().Should().Be(matrix.Id);
        requirements.Select(requirement => (requirement!["competencyId"]!.GetValue<Guid>(), requirement["requiredScore"]!.GetValue<int>(), requirement["type"]!.GetValue<string>()))
            .Should().Equal((critical.Id, 3, "Critical"), (profile.Id, 2, "Additional"));
    }

    private static Task<ApiResponse> CreateAsync(ApiClient client, Guid unitId, Guid positionId) =>
        client.PostAsync("/api/v1/career/matrices", new { orgUnitId = unitId, positionId });

    private static async Task<Guid> PositionAsync(CareerOrg org, Guid unitId) => await org.Admin.PositionAsync(unitId, Scenarios.Unique("Должность"));

    private static JsonObject Content(Guid positionId, Guid critical, Guid profile, int weight = 100) => new()
    {
        ["levels"] = new JsonArray(new JsonObject { ["levelId"] = positionId, ["minOverallPercent"] = 60 }),
        ["competencies"] = new JsonArray(
            new JsonObject { ["competencyId"] = critical, ["blockId"] = null, ["type"] = "Critical" },
            new JsonObject { ["competencyId"] = profile, ["blockId"] = null, ["type"] = "Additional" }),
        ["blockWeights"] = new JsonArray(new JsonObject { ["levelId"] = positionId, ["blockId"] = null, ["percent"] = weight }),
        ["requirements"] = new JsonArray(
            new JsonObject { ["levelId"] = positionId, ["competencyId"] = critical, ["requiredScore"] = 3 },
            new JsonObject { ["levelId"] = positionId, ["competencyId"] = profile, ["requiredScore"] = 2 }),
    };

    private static async Task<ApiResponse> SaveAsync(CareerOrg org, Guid versionId, JsonNode content)
    {
        var current = (await org.Admin.GetAsync($"/api/v1/career/matrix-versions/{versionId}")).Expect(HttpStatusCode.OK);
        return await org.TeamHead.Client.PutAsync($"/api/v1/career/matrix-versions/{versionId}/content", content, current.ETag);
    }

    private static async Task<string[]> ProblemsAsync(CareerOrg org, Guid versionId) =>
        [.. (await org.TeamHead.Client.GetAsync($"/api/v1/career/matrix-versions/{versionId}/completeness")).Expect(HttpStatusCode.OK).Json!["problems"]!.AsArray()
            .Select(problem => problem!["code"]!.GetValue<string>())];

    private static async Task<ApiResponse> Transit(ApiClient who, Guid versionId, string action)
    {
        var path = $"/api/v1/career/matrix-versions/{versionId}";
        var publication = await who.GetAsync($"{path}/publication");
        var cascade = publication.Status == HttpStatusCode.OK ? $"?cascade={Uri.EscapeDataString(publication.Json!["cascadeTag"]!.GetValue<string>())}" : string.Empty;
        var current = (await who.GetAsync(path)).Expect(HttpStatusCode.OK);
        return await who.PostAsync($"{path}/{action}{cascade}", ifMatch: current.ETag);
    }
}
