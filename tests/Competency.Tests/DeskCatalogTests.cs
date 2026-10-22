using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// What the desk says about published matrices that went out of date, about draft catalogue entries awaiting approval, and that the set
/// of drafts a user is asked to approve is the set the server lets them approve.
/// </summary>
public sealed class DeskCatalogTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string LevelKind = "Level";

    [Fact]
    public async Task Ac126_Ac161_ALevelAddedToTheTrackAfterPublication_AsksTheHeadForANewVersion_UntilTheVersionIsStarted()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var published = kit.VersionId;
        var added = await org.Admin.MakeLevelAsync(kit.Track.Id);

        var stale = await org.TeamHead.Client.ItemsAsync();
        var hr = await org.Hr.Client.ItemsAsync();
        (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created);
        var started = await org.TeamHead.Client.ItemsAsync();

        var item = stale.ItemFor(kit.MatrixId);
        item.Kinds().Should().Equal("MatrixLevelsChanged");
        item["group"]!.GetValue<string>().Should().Be("MatricesToBuild");
        item["isReference"]!.GetValue<bool>().Should().BeFalse();
        item["target"]!["id"]!.GetValue<Guid>().Should().Be(published);
        item.FirstReason()["details"]!["addedLevels"]!.AsArray().Select(level => level!["id"]!.GetValue<Guid>()).Should().Equal(added.Id);
        item.FirstReason()["details"]!["number"]!.GetValue<int>().Should().Be(1);
        hr.ItemOf(kit.MatrixId).Should().BeNull("a published matrix that fell behind is its unit head's to renew");
        started.ItemFor(kit.MatrixId).Kinds().Should().Equal("VersionNotSubmitted");
    }

    [Fact]
    public async Task Ac126_RemovedAndMovedLevels_AreNamedSeparately_AndNothingChangedAsksForNothing()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);

        var unchanged = (await org.TeamHead.Client.ItemsAsync()).ItemOf(kit.MatrixId);
        var levels = (await org.Admin.GetAsync($"/api/v1/career/levels?trackId={kit.Track.Id}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();
        (await org.Admin.PutAsync($"/api/v1/career/tracks/{kit.Track.Id}/level-order", new { levelIds = levels.Reverse() }, (await org.Admin.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}")).ETag))
            .Expect(HttpStatusCode.OK);
        var moved = (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);

        unchanged.Should().BeNull();
        moved.Kinds().Should().Equal("MatrixLevelsChanged");
        moved.FirstReason()["details"]!["movedLevels"]!.AsArray().Select(level => level!["id"]!.GetValue<Guid>()).Should().BeEquivalentTo([kit.Junior.Id, kit.Senior.Id]);
    }

    [Fact]
    public async Task Ac127_Ac128_ADeactivatedCompetencyIsAnAction_AChangedOneOnlyAReference_AndTheActionHidesTheReference()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var profile = (await org.Admin.GetAsync($"/api/v1/career/competencies/{kit.Profile.Id}")).Expect(HttpStatusCode.OK);
        (await org.Admin.PutAsync(
            $"/api/v1/career/competencies/{kit.Profile.Id}",
            new { name = Scenarios.Unique("Новое"), description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)Guid.Parse(CareerScenarios.BaseScaleId) },
            profile.ETag)).Expect(HttpStatusCode.OK);

        var changed = (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);
        var critical = (await org.Admin.GetAsync($"/api/v1/career/competencies/{kit.Critical.Id}")).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/career/competencies/{kit.Critical.Id}/deactivate", ifMatch: critical.ETag)).Expect(HttpStatusCode.OK);
        var inactive = (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);

        changed.Kinds().Should().Equal("MatrixCompetencyChanged");
        changed["isReference"]!.GetValue<bool>().Should().BeTrue();
        changed["group"]!.GetValue<string>().Should().Be("ChangedCompetencies");
        changed.FirstReason()["details"]!["entries"]!.AsArray().Select(entry => entry!["id"]!.GetValue<Guid>()).Should().Equal(kit.Profile.Id);
        inactive.Kinds().Should().Equal("MatrixUsesInactive");
        inactive["isReference"]!.GetValue<bool>().Should().BeFalse();
        inactive["group"]!.GetValue<string>().Should().Be("MatricesToBuild");
        inactive.FirstReason()["details"]!["entries"]!.AsArray().Select(entry => entry!["id"]!.GetValue<Guid>()).Should().Equal(kit.Critical.Id);
    }

    [Fact]
    public async Task Ac28_Ac32_Ac129_ADraftAHeadCreated_WaitsForTheHrAndTheAdministrator_AndIsOnlyAReferenceForItsAuthor()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var draft = await org.TeamHead.Client.MakeCompetencyAsync();

        var author = (await org.TeamHead.Client.ItemsAsync()).ItemFor(draft.Id);
        var hr = (await org.Hr.Client.ItemsAsync()).ItemFor(draft.Id);
        var administrator = (await org.Admin.ItemsAsync()).ItemFor(draft.Id);
        var otherHr = (await org.SiblingHr.Client.ItemsAsync()).ItemFor(draft.Id);

        author.Kinds().Should().Equal("OwnDraftAwaitsApproval");
        author["isReference"]!.GetValue<bool>().Should().BeTrue();
        author["group"]!.GetValue<string>().Should().Be("OwnDrafts");
        author["object"]!["type"]!.GetValue<string>().Should().Be("Competency");
        author.FirstReason()["occurredAt"].Should().NotBeNull("a draft is dated by its creation");
        foreach (var asked in new[] { hr, administrator })
        {
            asked.Kinds().Should().Equal("DraftAwaitsApproval");
            asked["isReference"]!.GetValue<bool>().Should().BeFalse();
            asked["group"]!.GetValue<string>().Should().Be("DraftsToApprove");
            asked.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.TeamHead.Employee);
        }

        otherHr.Kinds().Should().Equal("DraftAwaitsApproval");
        (await org.Hr.Client.PostAsync(
            $"/api/v1/career/competencies/{draft.Id}/approve{await CascadeAsync(org.Hr.Client, $"/api/v1/career/competencies/{draft.Id}")}",
            ifMatch: (await org.Admin.GetAsync($"/api/v1/career/competencies/{draft.Id}")).ETag)).Expect(HttpStatusCode.OK);
        (await org.TeamHead.Client.ItemsAsync()).ItemOf(draft.Id).Should().BeNull("the author has nothing to wait for any more");
        foreach (var client in new[] { org.Hr.Client, org.Admin })
        {
            (await client.ItemsAsync()).ItemFor(draft.Id).Kinds().Should().Equal("UnusedCompetency");
        }
    }

    [Fact]
    public async Task Ac113_TheDraftsTheDeskAsksToApprove_AreTheDraftsTheServerLetsTheUserApprove_LevelsAside()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var direction = await org.Admin.MakeDirectionAsync();
        var head = org.TeamHead.Client;
        await head.MakeCompetencyAsync();
        (await head.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Шкала"), points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);
        var teamTrack = (await head.PostAsync("/api/v1/career/tracks", new { name = Scenarios.Unique("Трек"), ownerOrgUnitId = org.Team.Id, directionId = direction.Id })).Expect(HttpStatusCode.Created);
        await head.MakeLevelAsync(teamTrack.Id);
        var siblingTrack = (await org.SiblingHead.Client.PostAsync(
            "/api/v1/career/tracks",
            new { name = Scenarios.Unique("Трек"), ownerOrgUnitId = org.Sibling.Id, directionId = direction.Id })).Expect(HttpStatusCode.Created);

        foreach (var client in new[] { org.Hr.Client, org.SiblingHr.Client, org.Admin, org.TeamHead.Client, org.Plain.Client })
        {
            var offered = (await client.GetAsync("/api/v1/career/drafts")).Expect(HttpStatusCode.OK).Json!.AsArray()
                .Where(row => row!["kind"]!.GetValue<string>() != LevelKind)
                .Select(row => row!["id"]!.GetValue<Guid>());
            var asked = (await client.ItemsAsync()).Where(item => item.Kinds().Contains("DraftAwaitsApproval")).Select(item => item["object"]!["id"]!.GetValue<Guid>());

            asked.Should().BeEquivalentTo(offered);
        }

        (await org.Hr.Client.ItemsAsync()).ItemOf(teamTrack.Id).Should().NotBeNull();
        (await org.Hr.Client.ItemsAsync()).ItemOf(siblingTrack.Id).Should().BeNull("the sibling unit has an HR of its own");
        (await org.SiblingHr.Client.ItemsAsync()).ItemOf(siblingTrack.Id).Should().NotBeNull();
        (await org.SiblingHr.Client.ItemsAsync()).ItemOf(teamTrack.Id).Should().BeNull();
    }

    private static async Task<MatrixKit> PublishedAsync(CareerOrg org)
    {
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        return kit;
    }

    private static async Task<string> CascadeAsync(ApiClient client, string path)
    {
        var publication = await client.GetAsync($"{path}/publication");
        return publication.Status == HttpStatusCode.OK ? $"?cascade={Uri.EscapeDataString(publication.Json!["cascadeTag"]!.GetValue<string>())}" : string.Empty;
    }
}
