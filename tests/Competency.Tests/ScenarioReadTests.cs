using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The methods that read what the scenarios show: what a version changes, whether it is complete, the builder step it stopped at, the
/// matrices that use a catalogue entry and the entries nothing uses; and the defaults a save fills in.
/// </summary>
public sealed class ScenarioReadTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac63_TheChangesOfAFirstVersion_AreItsWholeContent_AndOfANextOneOnlyWhatDiffersFromThePublishedOne()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();

        var first = (await org.TeamHead.Client.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/changes")).Expect(HttpStatusCode.OK).Json!;
        await kit.PublishAsync();
        var second = (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created);
        var edited = kit.Content(seniorCriticalScore: 4);
        edited["competencies"]![0]!["type"] = "Profile";
        (await kit.SaveAsync(org.TeamHead.Client, edited, versionId: second.Id)).Expect(HttpStatusCode.OK);
        var changed = (await org.TeamHead.Client.GetAsync($"/api/v1/career/matrix-versions/{second.Id}/changes")).Expect(HttpStatusCode.OK).Json!;
        var withoutProfile = kit.Content(seniorCriticalScore: 4);
        withoutProfile["competencies"]!.AsArray().RemoveAt(1);
        var requirements = withoutProfile["requirements"]!.AsArray();
        foreach (var requirement in requirements.Where(item => item!["competencyId"]!.GetValue<Guid>() == kit.Profile.Id).ToList())
        {
            requirements.Remove(requirement);
        }

        (await kit.SaveAsync(org.TeamHead.Client, withoutProfile, versionId: second.Id)).Expect(HttpStatusCode.OK);
        var removed = (await org.TeamHead.Client.GetAsync($"/api/v1/career/matrix-versions/{second.Id}/changes")).Expect(HttpStatusCode.OK).Json!;

        first["previousNumber"].Should().BeNull();
        first["number"]!.GetValue<int>().Should().Be(1);
        Kinds(first).Should().BeEquivalentTo(
            Enumerable.Repeat("Level/Added", 2).Concat(Enumerable.Repeat("Competency/Added", 2)).Concat(Enumerable.Repeat("BlockWeight/Added", 2)).Concat(Enumerable.Repeat("Requirement/Added", 4)));
        changed["previousNumber"]!.GetValue<int>().Should().Be(1);
        changed["number"]!.GetValue<int>().Should().Be(2);
        Kinds(changed).Should().BeEquivalentTo(["Competency/Changed", "Requirement/Changed"]);
        Kinds(removed).Should().BeEquivalentTo(["Competency/Removed", "Requirement/Changed", "Requirement/Removed", "Requirement/Removed"]);
        (await org.Plain.Client.GetAsync($"/api/v1/career/matrix-versions/{second.Id}/changes")).Status.Should().Be(HttpStatusCode.NotFound, "a version in work is the participants' alone");
    }

    [Fact]
    public async Task Ac64_TheCompletenessEndpoint_ReportsWhatTheHandOverRefuses_WithTheSameMessagesAndNamedCodes()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var empty = await MatrixKit.CreateAsync(org);
        var badWeights = await MatrixKit.CreateAsync(org);
        var inactive = await MatrixKit.CreateAsync(org);
        var noPosition = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var complete = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var weights = badWeights.Content();
        weights["blockWeights"]![0]!["percent"] = 99.5m;
        weights["blockWeights"]!.AsArray().RemoveAt(1);
        (await badWeights.SaveAsync(org.TeamHead.Client, weights)).Expect(HttpStatusCode.OK);
        await inactive.PrepareAsync();
        (await org.Admin.PostAsync($"/api/v1/career/competencies/{inactive.Profile.Id}/deactivate", ifMatch: inactive.Profile.ETag)).Expect(HttpStatusCode.OK);
        var position = (await org.Admin.GetAsync($"/api/v1/career/tracks/{noPosition.Track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray()[0]!["positions"]![0]!["id"]!.GetValue<Guid>();
        var current = (await org.Admin.GetAsync($"/api/v1/positions/{position}")).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/positions/{position}/deactivate", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);

        var cases = new (MatrixKit Kit, string[] Codes)[]
        {
            (empty, ["NoLevels"]),
            (badWeights, ["BlockWeights", "BlockWeights"]),
            (inactive, ["InactiveEntry"]),
            (noPosition, ["LevelWithoutActivePosition"]),
            (complete, []),
        };

        foreach (var (kit, codes) in cases)
        {
            var report = (await kit.Author.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/completeness")).Expect(HttpStatusCode.OK).Json!["problems"]!.AsArray();
            var submit = await kit.TransitAsync(org.TeamHead.Client, "submit");

            report.Select(problem => problem!["code"]!.GetValue<string>()).Should().Equal(codes);
            if (codes.Length == 0)
            {
                submit.Status.Should().Be(HttpStatusCode.OK);
                continue;
            }

            submit.Status.Should().Be(HttpStatusCode.Conflict);
            submit.FieldErrors("content").Should().Equal(report.Select(problem => problem!["message"]!.GetValue<string>()));
        }

        var weighted = (await badWeights.Author.GetAsync($"/api/v1/career/matrix-versions/{badWeights.VersionId}/completeness")).Json!["problems"]!.AsArray();
        weighted.Select(problem => problem!["levelId"]!.GetValue<Guid>()).Should().BeEquivalentTo([badWeights.Junior.Id, badWeights.Senior.Id]);
        (await org.Plain.Client.GetAsync($"/api/v1/career/matrix-versions/{empty.VersionId}/completeness")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac57_Ac201_SavingFillsAMissingScoreWithTheLowestOfTheScale_AndAMissingTypeWithProfile_ButKeepsTheTypeTheVersionHad()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org);
        var scale = (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK).Json!;
        var lowest = scale["points"]!.AsArray().Min(point => point!["score"]!.GetValue<int>());
        var bare = kit.Content();
        bare["requirements"] = new JsonArray();
        foreach (var competency in bare["competencies"]!.AsArray())
        {
            competency!.AsObject().Remove("type");
        }

        var first = (await kit.SaveAsync(org.TeamHead.Client, bare)).Expect(HttpStatusCode.OK).Json!["content"]!;
        bare["competencies"]![0]!["type"] = "Critical";
        var typed = (await kit.SaveAsync(org.TeamHead.Client, bare)).Expect(HttpStatusCode.OK).Json!["content"]!;
        bare["competencies"]![0]!.AsObject().Remove("type");
        var kept = (await kit.SaveAsync(org.TeamHead.Client, bare)).Expect(HttpStatusCode.OK).Json!["content"]!;

        first["requirements"]!.AsArray().Should().HaveCount(4, "every level has a score for every competency");
        first["requirements"]!.AsArray().Select(requirement => requirement!["requiredScore"]!.GetValue<int>()).Should().OnlyContain(score => score == lowest);
        first["competencies"]!.AsArray().Select(competency => competency!["type"]!.GetValue<string>()).Should().OnlyContain(type => type == "Profile");
        typed["competencies"]![0]!["type"]!.GetValue<string>().Should().Be("Critical");
        kept["competencies"]![0]!["type"]!.GetValue<string>().Should().Be("Critical", "a type not sent is the one the version had");
    }

    [Fact]
    public async Task Ac53_TheBuilderStepOfASave_IsKeptWithTheVersion_AndOnlyStepsTwoToSixAreAccepted()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org);
        string Path(string query) => $"/api/v1/career/matrix-versions/{kit.VersionId}/content{query}";
        async Task<ApiResponse> SaveAtAsync(string query) => await org.TeamHead.Client.PutAsync(Path(query), kit.Content(), await kit.ETagAsync());

        var third = (await SaveAtAsync("?builderStep=3")).Expect(HttpStatusCode.OK);
        var kept = (await SaveAtAsync(string.Empty)).Expect(HttpStatusCode.OK);
        var sixth = (await SaveAtAsync("?builderStep=6")).Expect(HttpStatusCode.OK);
        var second = (await SaveAtAsync("?builderStep=2")).Expect(HttpStatusCode.OK);
        var below = await SaveAtAsync("?builderStep=1");
        var above = await SaveAtAsync("?builderStep=7");
        var text = await SaveAtAsync("?builderStep=two");

        third.Json!["builderStep"]!.GetValue<int>().Should().Be(3);
        kept.Json!["builderStep"]!.GetValue<int>().Should().Be(3, "a save without a step leaves the step as it was");
        sixth.Json!["builderStep"]!.GetValue<int>().Should().Be(6);
        second.Json!["builderStep"]!.GetValue<int>().Should().Be(2);
        foreach (var refused in new[] { below, above })
        {
            refused.Status.Should().Be(HttpStatusCode.BadRequest);
            refused.FieldErrors("builderStep").Should().NotBeEmpty();
        }

        text.Status.Should().Be(HttpStatusCode.BadRequest);
        (await kit.GetVersionAsync(org.Admin)).Json!["builderStep"]!.GetValue<int>().Should().Be(2, "a refused save changes nothing");
        (await org.Admin.AuditOfRequestAsync(sixth.RequestId!)).Actions().Should().BeEmpty("a step alone is not journaled, nor is a document that stayed the same");
    }

    [Fact]
    public async Task Ac73_TheMatricesUsingAnEntry_AreListedToWhoeverMayReadTheirVersionsAlone_ArchivedOnesLeftOut()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var path = $"/api/v1/career/directions/{kit.Direction.Id}/matrices";

        var draftForHead = await RowsAsync(org.TeamHead.Client, path);
        var draftForPlain = await RowsAsync(org.Plain.Client, path);
        var draftForSibling = await RowsAsync(org.SiblingHead.Client, path);
        var draftForHr = await RowsAsync(org.Hr.Client, path);
        await kit.PublishAsync();
        var published = await RowsAsync(org.Plain.Client, path);
        var next = (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created);
        var forHead = await RowsAsync(org.TeamHead.Client, path);
        var forPlain = await RowsAsync(org.Plain.Client, path);
        var byLevel = await RowsAsync(org.Plain.Client, $"/api/v1/career/levels/{kit.Junior.Id}/matrices");
        var byCompetency = await RowsAsync(org.Plain.Client, $"/api/v1/career/competencies/{kit.Critical.Id}/matrices");
        var byScale = await RowsAsync(org.Plain.Client, $"/api/v1/career/scales/{CareerScenarios.BaseScaleId}/matrices");
        (await kit.TransitAsync(org.TeamHead.Client, "submit", versionId: next.Id)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve", versionId: next.Id)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "approve", versionId: next.Id)).Expect(HttpStatusCode.OK);
        var afterSecond = await RowsAsync(org.Plain.Client, path);

        draftForHead.Should().Equal($"{kit.MatrixId}/1/Draft");
        draftForHr.Should().Equal($"{kit.MatrixId}/1/Draft");
        draftForPlain.Should().BeEmpty("a version in work is not named to a user who may not read it");
        draftForSibling.Should().BeEmpty();
        published.Should().Equal($"{kit.MatrixId}/1/Published");
        forHead.Should().Equal($"{kit.MatrixId}/2/Draft", $"{kit.MatrixId}/1/Published");
        forPlain.Should().Equal($"{kit.MatrixId}/1/Published");
        byLevel.Should().Equal($"{kit.MatrixId}/1/Published");
        byCompetency.Should().Equal($"{kit.MatrixId}/1/Published");
        byScale.Should().Contain($"{kit.MatrixId}/1/Published");
        next.Json!["number"]!.GetValue<int>().Should().Be(2);
        afterSecond.Should().Equal($"{kit.MatrixId}/2/Published");
        (await org.Plain.Client.GetAsync($"/api/v1/career/directions/{Guid.NewGuid()}/matrices")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac137_Ac138_Ac140_TheEntriesNothingUses_AreListedToHrAndAdministratorsAlone_ByKind()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var competency = await org.Admin.MakeCompetencyAsync();
        var scale = await org.Admin.MakeScaleAsync();
        var direction = await org.Admin.MakeDirectionAsync();

        var all = (await org.Hr.Client.GetAsync("/api/v1/career/unused-entries?pageSize=200")).Expect(HttpStatusCode.OK);
        var competencies = (await org.Admin.GetAsync("/api/v1/career/unused-entries?kind=Competency&pageSize=200")).Expect(HttpStatusCode.OK);
        var paged = (await org.Admin.GetAsync("/api/v1/career/unused-entries?pageSize=1")).Expect(HttpStatusCode.OK);

        var ids = all.ItemIds();
        ids.Should().Contain([competency.Id.ToString(), scale.Id.ToString(), direction.Id.ToString()]);
        ids.Should().NotContain([kit.Critical.Id.ToString(), kit.Direction.Id.ToString(), CareerScenarios.BaseScaleId], "a matrix uses them, or the scale is the default");
        all.Json!["items"]!.AsArray().Single(item => item!["id"]!.GetValue<Guid>() == scale.Id)!["kind"]!.GetValue<string>().Should().Be("Scale");
        competencies.Json!["items"]!.AsArray().Select(item => item!["kind"]!.GetValue<string>()).Should().OnlyContain(kind => kind == "Competency");
        competencies.ItemIds().Should().Contain(competency.Id.ToString());
        paged.Json!["items"]!.AsArray().Should().ContainSingle();
        paged.Json["total"]!.GetValue<int>().Should().Be(all.Json["total"]!.GetValue<int>());
        foreach (var refused in new[] { org.TeamHead.Client, org.Plain.Client })
        {
            (await refused.GetAsync("/api/v1/career/unused-entries")).Status.Should().Be(HttpStatusCode.Forbidden);
        }

        (await org.Admin.GetAsync("/api/v1/career/unused-entries?kind=Nonsense")).Status.Should().Be(HttpStatusCode.BadRequest);
        (await org.Admin.GetAsync("/api/v1/career/unused-entries?page=0")).Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ac137_Ac138_Ac140_EachKindFilterOfTheUnusedEntries_ListsOnlyThatKind_WithTheEntrysVersion_AndTheTotalsOfTheKindsAddUp()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var competency = await org.Admin.MakeCompetencyAsync();
        var scale = await org.Admin.MakeScaleAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var renamed = new Dictionary<string, ApiResponse>
        {
            ["Competency"] = (await org.Admin.PutAsync(
                $"/api/v1/career/competencies/{competency.Id}",
                new { name = Scenarios.Unique("Правка"), description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)null },
                competency.ETag)).Expect(HttpStatusCode.OK),
            ["Scale"] = (await org.Admin.PutAsync(
                $"/api/v1/career/scales/{scale.Id}",
                new { name = Scenarios.Unique("Правка"), points = CareerScenarios.Points() },
                scale.ETag)).Expect(HttpStatusCode.OK),
            ["Direction"] = (await org.Admin.PutAsync(
                $"/api/v1/career/directions/{direction.Id}",
                new { name = Scenarios.Unique("Правка") },
                direction.ETag)).Expect(HttpStatusCode.OK),
        };

        var everything = (await org.Admin.GetAsync("/api/v1/career/unused-entries?pageSize=200")).Expect(HttpStatusCode.OK).Json!;
        var totals = 0;
        foreach (var (kind, entry) in renamed)
        {
            var listed = (await org.Admin.GetAsync($"/api/v1/career/unused-entries?kind={kind}&pageSize=200")).Expect(HttpStatusCode.OK).Json!;
            var items = listed["items"]!.AsArray();
            var row = items.Single(item => item!["id"]!.GetValue<Guid>() == entry.Id)!;

            items.Select(item => item!["kind"]!.GetValue<string>()).Should().OnlyContain(itemKind => itemKind == kind, kind);
            listed["total"]!.GetValue<int>().Should().Be(items.Count, kind);
            row["name"]!.GetValue<string>().Should().Be(entry.Json!["name"]!.GetValue<string>(), kind);
            $"\"{row["version"]!.GetValue<int>()}\"".Should().Be(entry.ETag, $"{kind}: the row carries the version the entry is changed with");
            totals += listed["total"]!.GetValue<int>();
        }

        everything["total"]!.GetValue<int>().Should().Be(totals, "every unused entry is of exactly one of the three kinds");
    }

    private static string[] Kinds(JsonNode diff) =>
        [.. diff["changes"]!.AsArray().Select(change => $"{change!["kind"]!.GetValue<string>()}/{change["type"]!.GetValue<string>()}")];

    private static async Task<string[]> RowsAsync(ApiClient client, string path) =>
        [.. (await client.GetAsync(path)).Expect(HttpStatusCode.OK).Json!.AsArray().Select(row => $"{row!["matrixId"]!.GetValue<Guid>()}/{row["number"]!.GetValue<int>()}/{row["status"]!.GetValue<string>()}")];
}
