using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Creating a matrix and editing the document of its version: only the head of a unit creates its matrices, for a track of the unit or of a unit
/// above it, one per unit and track; the document is checked on its own and against the catalogues, and a stale edit is refused.
/// </summary>
public sealed class MatrixCreationTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string UnavailableMessage = "Запись не найдена или недоступна";

    [Fact]
    public async Task Ac14_Create_BuildsTheMatrixWithAnEmptyDraftVersion_NamingTheUnitDirectionAndTrack()
    {
        var org = await fixture.OrgAsync();

        var kit = await MatrixKit.CreateAsync(org);

        var inWork = kit.Matrix.Json!["inWorkVersion"]!;
        inWork["number"]!.GetValue<int>().Should().Be(1);
        inWork["status"]!.GetValue<string>().Should().Be("Draft");
        kit.Matrix.Json["publishedVersion"].Should().BeNull();
        kit.Matrix.Json["orgUnitName"]!.GetValue<string>().Should().Be(org.Team.Json!["name"]!.GetValue<string>());
        kit.Matrix.Json["directionName"]!.GetValue<string>().Should().Be(kit.Direction.Json!["name"]!.GetValue<string>());
        kit.Matrix.Json["trackName"]!.GetValue<string>().Should().Be(kit.Track.Json!["name"]!.GetValue<string>());
        var version = (await kit.GetVersionAsync(org.TeamHead.Client)).Expect(HttpStatusCode.OK);
        version.Json!["content"]!["levels"]!.AsArray().Should().BeEmpty();
        version.Json["authorId"]!.GetValue<Guid>().Should().Be(org.TeamHead.Id);
        MatrixKit.Actions(version).Should().BeEquivalentTo(["Edit", "Submit"]);
    }

    [Fact]
    public async Task Ac14_Create_IsTheUnitHeadsOnly()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);

        foreach (var member in new[] { org.Hr, org.DepartmentHead, org.SiblingHead, org.Plain })
        {
            var refused = await member.Client.PostAsync("/api/v1/career/matrices", Request(org.Team.Id, direction.Id, track.Id));
            refused.Status.Should().Be(HttpStatusCode.Forbidden, member.User.Email);
        }

        (await org.Admin.PostAsync("/api/v1/career/matrices", Request(org.Team.Id, direction.Id, track.Id))).Status.Should().Be(HttpStatusCode.Forbidden, "an administrator does not author matrices");
    }

    [Fact]
    public async Task Ac14_Create_AllowsATrackOfTheUnitOrOfAUnitAbove_AndRefusesATrackOfAUnitBelowOrBeside()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var ofDepartment = await org.Admin.MakeActiveTrackAsync(org.Department.Id, direction.Id);
        var ofTeam = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var ofSibling = await org.Admin.MakeActiveTrackAsync(org.Sibling.Id, direction.Id);

        var teamOnDepartmentsTrack = await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", Request(org.Team.Id, direction.Id, ofDepartment.Id));
        var siblingOnDepartmentsTrack = await org.SiblingHead.Client.PostAsync("/api/v1/career/matrices", Request(org.Sibling.Id, direction.Id, ofDepartment.Id));
        var departmentOnTeamsTrack = await org.DepartmentHead.Client.PostAsync("/api/v1/career/matrices", Request(org.Department.Id, direction.Id, ofTeam.Id));
        var teamOnSiblingsTrack = await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", Request(org.Team.Id, direction.Id, ofSibling.Id));

        teamOnDepartmentsTrack.Status.Should().Be(HttpStatusCode.Created, teamOnDepartmentsTrack.Body);
        siblingOnDepartmentsTrack.Status.Should().Be(HttpStatusCode.Created, siblingOnDepartmentsTrack.Body);
        departmentOnTeamsTrack.FieldErrors("trackId").Should().Equal([UnavailableMessage]);
        teamOnSiblingsTrack.FieldErrors("trackId").Should().Equal([UnavailableMessage]);
    }

    [Fact]
    public async Task Ac14_Create_AllowsOneMatrixPerUnitAndTrack()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);

        var again = await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", Request(org.Team.Id, kit.Direction.Id, kit.Track.Id));
        var otherTrack = await org.Admin.MakeActiveTrackAsync(org.Team.Id, kit.Direction.Id);
        var next = await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", Request(org.Team.Id, kit.Direction.Id, otherTrack.Id));

        again.Status.Should().Be(HttpStatusCode.Conflict);
        again.FieldErrors("trackId").Should().Equal(["У подразделения уже есть матрица для этого трека"]);
        next.Status.Should().Be(HttpStatusCode.Created, next.Body);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM competency_matrices WHERE org_unit_id = '{org.Team.Id}' AND track_id = '{kit.Track.Id}'")).Should().Be(1);
    }

    [Fact]
    public async Task Ac14_Create_NeedsAnActiveDirectionAndAnActiveTrackOfIt()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var other = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var inactiveDirection = await org.Admin.MakeDirectionAsync();
        (await org.Admin.PostAsync($"/api/v1/career/directions/{inactiveDirection.Id}/deactivate", ifMatch: inactiveDirection.ETag)).Expect(HttpStatusCode.OK);
        var inactiveTrack = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        (await org.Admin.PostAsync($"/api/v1/career/tracks/{inactiveTrack.Id}/deactivate", ifMatch: inactiveTrack.ETag)).Expect(HttpStatusCode.OK);
        var unknownDirection = await Create(org, Guid.NewGuid(), track.Id);
        var inactiveDirectionRequest = await Create(org, inactiveDirection.Id, track.Id);
        var wrongDirection = await Create(org, other.Id, track.Id);
        var inactiveTrackRequest = await Create(org, direction.Id, inactiveTrack.Id);
        var unknownTrack = await Create(org, direction.Id, Guid.NewGuid());
        var valid = await Create(org, direction.Id, track.Id);

        unknownDirection.FieldErrors("directionId").Should().Equal([UnavailableMessage]);
        inactiveDirectionRequest.FieldErrors("directionId").Should().Equal([UnavailableMessage]);
        wrongDirection.FieldErrors("trackId").Should().Equal([UnavailableMessage]);
        inactiveTrackRequest.FieldErrors("trackId").Should().Equal([UnavailableMessage]);
        unknownTrack.FieldErrors("trackId").Should().Equal([UnavailableMessage]);
        valid.Status.Should().Be(HttpStatusCode.Created, valid.Body);
    }

    [Fact]
    public async Task Ac14_Document_IsStoredAsSent_AndReadBackWithItsCompetenciesWeightsAndRequiredScores()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);
        var content = kit.Content();

        var saved = (await kit.SaveAsync(org.TeamHead.Client, content)).Expect(HttpStatusCode.OK);

        var stored = saved.Json!["content"]!;
        stored["requirements"]!.AsArray().Select(item => item!["requiredScore"]!.GetValue<int>()).Should().Equal(2, 1, 3, 2);
        stored["competencies"]!.AsArray().Select(item => item!["type"]!.GetValue<string>()).Should().Equal("Critical", "Additional");
        stored["levels"]!.AsArray().Select(level => level!["minOverallPercent"]!.GetValue<decimal>()).Should().Equal(60m, 70m);
        stored["blockWeights"]!.AsArray().Should().HaveCount(2);
        saved.Json["entries"]!["levels"]!.AsArray().Select(level => level!["name"]!.GetValue<string>())
            .Should().Equal([kit.Junior.Json!["name"]!.GetValue<string>(), kit.Senior.Json!["name"]!.GetValue<string>()], "levels are listed in the order of the track");
        saved.Json["entries"]!["competencies"]!.AsArray().Select(c => c!["name"]!.GetValue<string>())
            .Should().Equal(kit.Critical.Json!["name"]!.GetValue<string>(), kit.Profile.Json!["name"]!.GetValue<string>());
    }

    [Fact]
    public async Task Ac14_Document_IsRefusedWhenItIsNotWellFormed()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);
        var cases = new (string Reason, Action<JsonObject> Break, string Message)[]
        {
            ("a repeated level", c => c["levels"]!.AsArray().Add(c["levels"]![0]!.DeepClone()), "Уровни, блоки, компетенции и требования не должны повторяться"),
            ("a repeated requirement", c => c["requirements"]!.AsArray().Add(c["requirements"]![0]!.DeepClone()), "Уровни, блоки, компетенции и требования не должны повторяться"),
            ("a competency in a block of its own", c => c["competencies"]![0]!["blockId"] = Guid.NewGuid(), "Собственных блоков у версии нет: компетенцию добавляют вне блоков или в составе общего блока"),
            ("a coefficient of a level outside the version", c => c["blockWeights"]![0]!["levelId"] = Guid.NewGuid(), "Коэффициент относится к уровню или блоку не из версии"),
            ("a requirement of a competency outside the version", c => c["requirements"]![0]!["competencyId"] = Guid.NewGuid(), "Требование относится к уровню или компетенции не из версии"),
            ("a threshold above 100", c => c["levels"]![0]!["minOverallPercent"] = 100.5m, "Коэффициенты, веса и пороги — от 0 до 100 %"),
            ("a negative coefficient", c => c["blockWeights"]![0]!["percent"] = -1, "Коэффициенты, веса и пороги — от 0 до 100 %"),
            ("a competency type that names no type", c => c["competencies"]![0]!["type"] = 999, "Укажите допустимый тип компетенции"),
            ("a type that names no type inside a shared block", c => c["sharedBlocks"] = new JsonArray(new JsonObject
            {
                ["blockId"] = Guid.NewGuid(),
                ["competencyTypes"] = new JsonArray(new JsonObject { ["competencyId"] = Guid.NewGuid(), ["type"] = 999 }),
            }), "Укажите допустимый тип компетенции"),
        };

        foreach (var (reason, breakIt, message) in cases)
        {
            var content = kit.Content();
            breakIt(content);

            var refused = await kit.SaveAsync(org.TeamHead.Client, content);

            refused.Status.Should().Be(HttpStatusCode.BadRequest, reason);
            refused.FieldErrors("content").Should().Equal([message], reason);
        }

        (await kit.GetVersionAsync(org.Admin)).Json!["content"]!["levels"]!.AsArray().Should().BeEmpty("a refused document changes nothing");
    }

    [Fact]
    public async Task Ac14_Document_MayReferOnlyToTheTrackLevelsAndToCompetenciesOfTheDirection()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);
        var otherTrack = await org.Admin.MakeActiveTrackAsync(org.Team.Id, kit.Direction.Id);
        var foreignLevel = await org.Admin.MakeLevelAsync(otherTrack.Id);
        var otherDirection = await org.Admin.MakeDirectionAsync();
        var ofOtherDirection = await org.Admin.MakeCompetencyAsync(directionId: otherDirection.Id);
        var inactive = await org.Admin.MakeCompetencyAsync(directionId: kit.Direction.Id);
        (await org.Admin.PostAsync($"/api/v1/career/competencies/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);
        var othersDraft = (await org.SiblingHead.Client.PostAsync(
            "/api/v1/career/competencies",
            new { name = Scenarios.Unique("Чужая"), description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)null })).Expect(HttpStatusCode.Created);

        var baseScale = (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK);
        var cases = new (string Reason, Action<JsonObject> Change, string Message)[]
        {
            ("a level of another track", c => c["levels"]!.AsArray().Add(new JsonObject { ["levelId"] = foreignLevel.Id, ["minOverallPercent"] = 50 }), "Уровень не из трека матрицы"),
            ("an unknown level", c => c["levels"]!.AsArray().Add(new JsonObject { ["levelId"] = Guid.NewGuid(), ["minOverallPercent"] = 50 }), "Уровень не из трека матрицы"),
            ("a competency of another direction", c => c["competencies"]!.AsArray().Add(Competency(ofOtherDirection.Id)), "Компетенция не из области матрицы"),
            ("an unknown competency", c => c["competencies"]!.AsArray().Add(Competency(Guid.NewGuid())), UnavailableMessage),
            ("an inactive competency", c => c["competencies"]!.AsArray().Add(Competency(inactive.Id)), UnavailableMessage),
            ("a draft of another author", c => c["competencies"]!.AsArray().Add(Competency(othersDraft.Id)), UnavailableMessage),
            ("a score the scale does not have", c => c["requirements"]![0]!["requiredScore"] = 9, $"Требуемого уровня нет в шкале «{baseScale.Json!["name"]!.GetValue<string>()}»"),
        };

        foreach (var (reason, change, message) in cases)
        {
            var content = kit.Content();
            change(content);

            var refused = await kit.SaveAsync(org.TeamHead.Client, content);

            refused.Status.Should().Be(HttpStatusCode.BadRequest, reason);
            refused.FieldErrors("content").Should().Equal([message], reason);
        }
    }

    [Fact]
    public async Task Ac17_Edit_WithAStaleVersion_IsRefusedWith412_AndTheFirstEditStands()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);
        var opened = await kit.ETagAsync();
        (await kit.SaveAsync(org.TeamHead.Client, kit.Content(4), opened)).Expect(HttpStatusCode.OK);

        var stale = await kit.SaveAsync(org.TeamHead.Client, kit.Content(1), opened);

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        (await kit.GetVersionAsync(org.Admin)).Json!["content"]!["requirements"]![2]!["requiredScore"]!.GetValue<int>().Should().Be(4);
    }

    [Fact]
    public async Task Ac17_RepeatingAnEditWithTheTagItUsed_IsRefusedWith412_EvenThoughTheDocumentIsTheSame_WhileTheCurrentTagKeepsTheVersion()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);
        var opened = await kit.ETagAsync();
        var first = (await kit.SaveAsync(org.TeamHead.Client, kit.Content(4), opened)).Expect(HttpStatusCode.OK);

        var stale = await kit.SaveAsync(org.TeamHead.Client, kit.Content(4), opened);
        var current = await kit.SaveAsync(org.TeamHead.Client, kit.Content(4), first.ETag);

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        current.Status.Should().Be(HttpStatusCode.OK, current.Body);
        current.ETag.Should().Be(first.ETag, "a document that changes nothing leaves the version as it was");
    }

    [Fact]
    public async Task Ac17_EditingTheDocument_ChangesTheVersionEntityTag_AndSavingTheSameDocumentDoesNot()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);
        var before = await kit.ETagAsync();

        var changed = (await kit.SaveAsync(org.TeamHead.Client, kit.Content(), before)).Expect(HttpStatusCode.OK);
        var same = (await kit.SaveAsync(org.TeamHead.Client, kit.Content(), changed.ETag)).Expect(HttpStatusCode.OK);

        changed.ETag.Should().NotBe(before);
        same.ETag.Should().Be(changed.ETag, "a save that changes nothing is not a change");
    }

    private static JsonObject Competency(Guid competencyId) => new() { ["competencyId"] = competencyId, ["blockId"] = null };

    private static object Request(Guid unitId, Guid directionId, Guid trackId) =>
        new { orgUnitId = unitId, directionId, trackId };

    private static Task<ApiResponse> Create(CareerOrg org, Guid directionId, Guid trackId) =>
        org.TeamHead.Client.PostAsync("/api/v1/career/matrices", Request(org.Team.Id, directionId, trackId));
}
