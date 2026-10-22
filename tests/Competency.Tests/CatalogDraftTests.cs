using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The list of drafts that wait for approval: an administrator sees every draft, an HR the drafts of no unit and the draft tracks of the units they
/// answer for, and anyone else an empty list; draft levels go with their track, so they are not listed.
/// </summary>
public sealed class CatalogDraftTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac9_Drafts_AreListedToAdministratorsAndHr_EachSeeingWhatTheyMayApprove()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var drafts = await DraftsAsync(org);

        var forAdmin = await ListAsync(org.Admin);
        var forHr = await ListAsync(org.Hr.Client);
        var forSiblingHr = await ListAsync(org.SiblingHr.Client);

        forAdmin.Select(row => row.Id).Should().Contain(drafts.All);
        forHr.Select(row => row.Id).Should().Contain(drafts.OfNoUnit).And.Contain(drafts.TeamTrack).And.NotContain(drafts.SiblingTrack);
        forSiblingHr.Select(row => row.Id).Should().Contain(drafts.OfNoUnit).And.Contain(drafts.SiblingTrack).And.NotContain(drafts.TeamTrack);
        foreach (var list in new[] { forAdmin, forHr, forSiblingHr })
        {
            list.Select(row => row.Id).Should().NotContain(drafts.Level, "a draft level is approved with its track");
        }
    }

    [Fact]
    public async Task Ac9_Drafts_AreEmptyForHeadsAndOtherUsers_EvenForTheirOwn()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        await DraftsAsync(org);

        (await ListAsync(org.TeamHead.Client)).Should().BeEmpty();
        (await ListAsync(org.Plain.Client)).Should().BeEmpty();
        (await fixture.Host.Anonymous().GetAsync("/api/v1/career/drafts")).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ac24_Drafts_AreFilteredByKindAndUnit_OldestFirst_WithTheirAuthorsAndUnits()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var drafts = await DraftsAsync(org);

        var tracks = await ListAsync(org.Admin, "kind=Track");
        var ofSibling = await ListAsync(org.Admin, $"orgUnitId={org.Sibling.Id}");
        var scales = await ListAsync(org.Hr.Client, "kind=Scale");

        tracks.Select(row => row.Kind).Should().OnlyContain(kind => kind == "Track");
        tracks.Select(row => row.Id).Should().Contain([drafts.TeamTrack, drafts.SiblingTrack]);
        ofSibling.Select(row => row.Id).Should().Equal(drafts.SiblingTrack);
        scales.Select(row => row.Id).Should().Contain(drafts.Scale);
        var teamTrack = tracks.Single(row => row.Id == drafts.TeamTrack);
        teamTrack.UnitName.Should().Be(org.Team.Json!["name"]!.GetValue<string>());
        teamTrack.AuthorId.Should().Be(org.TeamHead.Id);
        teamTrack.AuthorName.Should().NotBeNullOrEmpty();
        (await ListAsync(org.Admin)).Select(row => row.CreatedAt).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Ac9_AnApprovedOrDeletedDraft_LeavesTheList()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var approved = (await org.TeamHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Одобренное") })).Expect(HttpStatusCode.Created);
        var deleted = (await org.TeamHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Удалённое") })).Expect(HttpStatusCode.Created);
        (await org.Hr.Client.PostAsync($"/api/v1/career/directions/{approved.Id}/approve", ifMatch: approved.ETag)).Expect(HttpStatusCode.OK);
        (await org.TeamHead.Client.DeleteAsync($"/api/v1/career/directions/{deleted.Id}", deleted.ETag)).Expect(HttpStatusCode.NoContent);

        var listed = (await ListAsync(org.Hr.Client)).Select(row => row.Id);

        listed.Should().NotContain([approved.Id, deleted.Id]);
    }

    [Fact]
    public async Task Ac17_ARowOfTheDraftList_CarriesTheTagOfTheEntry_AndApprovalThatSendsTheTagShownBeforeALaterChange_IsRefused()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var direction = await org.Admin.MakeDirectionAsync();
        var head = org.TeamHead.Client;
        var readyTrack = await head.MakeTrackAsync(org.Team.Id, direction.Id);
        await org.Admin.LinkToNewPositionAsync(await org.Admin.MakeLevelAsync(readyTrack.Id), org.Team.Id);
        var drafts = new[]
        {
            ("directions", (await head.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Направление") })).Expect(HttpStatusCode.Created)),
            ("tracks", (await org.Admin.GetAsync($"/api/v1/career/tracks/{readyTrack.Id}")).Expect(HttpStatusCode.OK)),
        };

        foreach (var (segment, draft) in drafts)
        {
            var path = $"/api/v1/career/{segment}/{draft.Id}";

            async Task<int?> ShownAsync() =>
                (await org.Hr.Client.GetAsync("/api/v1/career/drafts")).Expect(HttpStatusCode.OK).Json!.AsArray()
                    .Single(row => row!["id"]!.GetValue<Guid>() == draft.Id)!["version"]?.GetValue<int>();

            var shown = await ShownAsync();
            shown.Should().NotBeNull(segment);
            $"\"{shown}\"".Should().Be(draft.ETag, $"{segment}: the row carries the tag the entry is approved with");
            var renamed = (await head.PutAsync(path, new { name = Scenarios.Unique("Правка") }, draft.ETag)).Expect(HttpStatusCode.OK);

            var stale = await org.Hr.Client.PostAsync($"{path}/approve{await CascadeAsync(org.Hr.Client, path)}", ifMatch: $"\"{shown}\"");

            stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, $"{segment}: {stale.Body}");
            (await org.Admin.GetAsync(path)).Expect(HttpStatusCode.OK).Json!["status"]!.GetValue<string>().Should().Be("Draft", segment);
            var current = await ShownAsync();
            $"\"{current}\"".Should().Be(renamed.ETag, segment);
            (await org.Hr.Client.PostAsync($"{path}/approve{await CascadeAsync(org.Hr.Client, path)}", ifMatch: $"\"{current}\"")).Expect(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Ac9_TheDraftList_ShowsEachDraftTrackItsOwnReadiness_AmongSeveralTracks()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var direction = await org.Admin.MakeDirectionAsync();
        var head = org.TeamHead.Client;
        var ready = await head.MakeTrackAsync(org.Team.Id, direction.Id);
        await org.Admin.LinkToNewPositionAsync(await head.MakeLevelAsync(ready.Id), org.Team.Id);
        var withoutLevels = await head.MakeTrackAsync(org.Team.Id, direction.Id);
        var partial = await head.MakeTrackAsync(org.Team.Id, direction.Id);
        await org.Admin.LinkToNewPositionAsync(await head.MakeLevelAsync(partial.Id), org.Team.Id);
        var bare = await head.MakeLevelAsync(partial.Id);

        var rows = (await org.Hr.Client.GetAsync("/api/v1/career/drafts?kind=Track")).Expect(HttpStatusCode.OK).Json!.AsArray();

        JsonNode? ReadinessOf(Guid trackId) => rows.Single(row => row!["id"]!.GetValue<Guid>() == trackId)!["readiness"];

        var withoutLevelsReadiness = ReadinessOf(withoutLevels.Id);
        var partialReadiness = ReadinessOf(partial.Id);

        ReadinessOf(ready.Id).Should().BeNull("a ready track has nothing to report");
        withoutLevelsReadiness.Should().NotBeNull("a track without levels is not ready");
        withoutLevelsReadiness!["hasLevels"]!.GetValue<bool>().Should().BeFalse();
        withoutLevelsReadiness["levelsWithoutActivePosition"]!.AsArray().Should().BeEmpty();
        partialReadiness.Should().NotBeNull("a track with a level without a position is not ready");
        partialReadiness!["hasLevels"]!.GetValue<bool>().Should().BeTrue();
        partialReadiness["levelsWithoutActivePosition"]!.AsArray().Select(level => level!["id"]!.GetValue<Guid>()).Should().Equal(bare.Id);
    }

    private static async Task<string> CascadeAsync(ApiClient client, string path)
    {
        var publication = await client.GetAsync($"{path}/publication");
        return publication.Status == HttpStatusCode.OK ? $"?cascade={Uri.EscapeDataString(publication.Json!["cascadeTag"]!.GetValue<string>())}" : string.Empty;
    }

    private async Task<Drafts> DraftsAsync(CareerOrg org)
    {
        var direction = await org.Admin.MakeDirectionAsync();
        var head = org.TeamHead.Client;
        var draftDirection = (await head.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Направление") })).Expect(HttpStatusCode.Created);
        var draftScale = (await head.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Шкала"), points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);
        var draftCompetency = await head.MakeCompetencyAsync();
        var teamTrack = (await head.PostAsync("/api/v1/career/tracks", new { name = Scenarios.Unique("Трек"), ownerOrgUnitId = org.Team.Id, directionId = direction.Id })).Expect(HttpStatusCode.Created);
        var siblingTrack = (await org.SiblingHead.Client.PostAsync("/api/v1/career/tracks", new { name = Scenarios.Unique("Трек"), ownerOrgUnitId = org.Sibling.Id, directionId = direction.Id })).Expect(HttpStatusCode.Created);
        var level = await head.MakeLevelAsync(teamTrack.Id);
        return new Drafts(
            [draftDirection.Id, draftScale.Id, draftCompetency.Id],
            draftScale.Id,
            teamTrack.Id,
            siblingTrack.Id,
            level.Id,
            [draftDirection.Id, draftScale.Id, draftCompetency.Id, teamTrack.Id, siblingTrack.Id]);
    }

    private static async Task<List<Draft>> ListAsync(ApiClient client, string query = "") =>
        [.. (await client.GetAsync($"/api/v1/career/drafts?{query}")).Expect(HttpStatusCode.OK).Json!.AsArray().Select(row => new Draft(
            row!["id"]!.GetValue<Guid>(),
            row["kind"]!.GetValue<string>(),
            row["ownerOrgUnitName"]?.GetValue<string>(),
            row["authorId"]?.GetValue<Guid>(),
            row["authorName"]?.GetValue<string>(),
            row["createdAt"]!.GetValue<DateTimeOffset>()))];

    private sealed record Draft(Guid Id, string Kind, string? UnitName, Guid? AuthorId, string? AuthorName, DateTimeOffset CreatedAt);

    private sealed record Drafts(Guid[] OfNoUnit, Guid Scale, Guid TeamTrack, Guid SiblingTrack, Guid Level, Guid[] All);
}
