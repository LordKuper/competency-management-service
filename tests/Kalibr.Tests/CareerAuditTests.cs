using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The journal records the creation and the changes of every career entity, a refused change leaves no row, a matrix version's document is never
/// written to it, and the system holds no financial data in its tables or its API.
/// </summary>
public sealed class CareerAuditTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac17_EveryCatalogueEntry_IsJournaledOnCreationAndOnRename_WithTheActorAndTheEntryIdentifier()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var created = new (string Type, ApiResponse Entry, string Path)[]
        {
            ("CareerDirection", direction, "directions"),
            ("CompetencyScale", await org.Admin.MakeScaleAsync(), "scales"),
            ("CareerCompetency", await org.Admin.MakeCompetencyAsync(), "competencies"),
            ("CareerTrack", await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id), "tracks"),
        };

        foreach (var (type, entry, path) in created)
        {
            var creation = (await org.Admin.AuditOfRequestAsync(entry.RequestId!)).Single(row => row!["entityType"]!.GetValue<string>() == type)!;
            creation["action"]!.GetValue<string>().Should().Be($"{type}.Created");
            creation["entityId"]!.GetValue<string>().Should().Be(entry.Id.ToString());
            creation["actor"]!.GetValue<string>().Should().Be((await org.Admin.GetAsync("/api/v1/auth/me")).Id.ToString());
            creation["role"]!.GetValue<string>().Should().Be(Scenarios.GlobalAdmin);
            creation["newValue"]!["name"]!.GetValue<string>().Should().Be(entry.Json!["name"]!.GetValue<string>());
            creation["newValue"]!["status"]!.GetValue<string>().Should().Be(type == "CareerTrack" ? "Draft" : "Active", "a track starts as a draft until it is approved");
            creation["newValue"]!.AsObject().ContainsKey("version").Should().BeFalse("only allow-listed properties are written");

            var body = path == "scales"
                ? (object)new { name = Scenarios.Unique("Новая"), points = CareerScenarios.Points() }
                : path == "competencies"
                    ? new { name = Scenarios.Unique("Новая"), description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)null }
                    : new { name = Scenarios.Unique("Новая") };
            var renamed = (await org.Admin.PutAsync($"/api/v1/career/{path}/{entry.Id}", body, entry.ETag)).Expect(HttpStatusCode.OK);
            var change = (await org.Admin.AuditOfRequestAsync(renamed.RequestId!)).Single(row => row!["entityType"]!.GetValue<string>() == type)!;
            change["action"]!.GetValue<string>().Should().Be($"{type}.Updated");
            change["oldValue"]!["name"]!.GetValue<string>().Should().Be(entry.Json["name"]!.GetValue<string>());
        }
    }

    [Fact]
    public async Task Ac17_ALevelAndItsPlaceInTheTrack_AreJournaled()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);

        var level = await org.Admin.MakeLevelAsync(track.Id);

        var creation = (await org.Admin.AuditOfRequestAsync(level.RequestId!)).Single()!;
        creation["action"]!.GetValue<string>().Should().Be("CareerLevel.Created");
        creation["newValue"]!["trackId"]!.GetValue<Guid>().Should().Be(track.Id);
        creation["newValue"]!["position"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task Ac17_ARefusedCatalogueChange_LeavesNoJournalRow()
    {
        var org = await fixture.OrgAsync();
        var name = Scenarios.Unique("Занято");
        var direction = (await org.Admin.PostAsync("/api/v1/career/directions", new { name })).Expect(HttpStatusCode.Created);

        var duplicate = await org.Admin.PostAsync("/api/v1/career/directions", new { name });
        var forbidden = await org.Plain.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Нельзя") });
        var invalid = await org.Admin.PostAsync("/api/v1/career/directions", new { name = " " });
        var stale = await org.Admin.PutAsync($"/api/v1/career/directions/{direction.Id}", new { name = Scenarios.Unique("Старое") }, "\"999\"");

        foreach (var refused in new[] { duplicate, forbidden, invalid, stale })
        {
            (await org.Admin.AuditOfRequestAsync(refused.RequestId!)).Should().BeEmpty(refused.Body);
        }
    }

    [Fact]
    public async Task Ac17_AMatrixAndItsVersions_AreJournaled_WithoutTheDocumentOfTheVersion()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);

        var creation = await org.Admin.AuditOfRequestAsync(kit.Matrix.RequestId!);
        var edited = (await kit.SaveAsync(org.TeamHead.Client, kit.Content(4))).Expect(HttpStatusCode.OK);
        var edit = await org.Admin.AuditOfRequestAsync(edited.RequestId!);

        creation.Actions().Should().Equal("CompetencyMatrix.Created", "MatrixVersion.Created");
        edit.Actions().Should().Equal("MatrixVersion.ContentUpdated");
        (await fixture.Host.ScalarAsync<long>("SELECT count(*) FROM audit_events WHERE concat_ws(' ', old_value::text, new_value::text, reason) LIKE '%blockWeights%'"))
            .Should().Be(0, "the document is not journaled");
    }

    [Fact]
    public async Task Ac11_NoColumnAndNoRoute_CarriesFinancialData()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var level = await org.Admin.MakeLevelAsync(track.Id);

        var columns = await fixture.Host.ScalarAsync<long>("SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public' AND column_name ~* '(salary|budget|wage|оклад)'");
        var read = await org.Admin.GetAsync($"/api/v1/career/levels/{level.Id}/salary");
        var write = await org.Admin.PutAsync($"/api/v1/career/levels/{level.Id}/salary", new { salaryFrom = 1, salaryTo = 2 }, level.ETag);

        columns.Should().Be(0);
        read.Status.Should().Be(HttpStatusCode.NotFound);
        write.Code.Should().BeInRange(400, 499, "there is no route to write a salary range");
        level.Json!.AsObject().Select(property => property.Key).Should().NotContain(key => key.Contains("salary", StringComparison.OrdinalIgnoreCase));
    }
}
