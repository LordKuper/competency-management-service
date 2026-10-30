using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// What the desk says about tracks, their levels and the positions linked to them, about positions and employees left without a track,
/// and about units that still lack a matrix: who is asked to act, who is only told, and what takes an item away.
/// </summary>
public sealed class DeskTrackTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac123_Ac124_Ac125_Ac134_ADraftTrack_AsksTheHeadToFinishItsLevelsAndPositions_AndMarksTheApprovalEarlyForTheHr()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, (await org.Admin.MakeDirectionAsync()).Id);

        var withoutLevels = await TrackItemsAsync(org, track);
        var first = await org.Admin.MakeLevelAsync(track.Id);
        var oneLevel = await TrackItemsAsync(org, track);
        var linked = await org.Admin.LinkToNewPositionAsync(first, org.Team.Id);
        var oneLevelLinked = await TrackItemsAsync(org, track);
        var second = await org.Admin.MakeLevelAsync(track.Id);
        var gap = await TrackItemsAsync(org, track);
        await org.Admin.LinkToNewPositionAsync(second, org.Team.Id);
        var readyHr = (await org.Hr.Client.ItemsAsync()).ItemFor(track.Id);
        var readyHead = (await org.TeamHead.Client.ItemsAsync()).ItemOf(track.Id);

        withoutLevels.Head.Kinds().Should().Equal("TrackWithoutLevels");
        withoutLevels.Head["group"]!.GetValue<string>().Should().Be("PositionsToLink");
        withoutLevels.Head["isReference"]!.GetValue<bool>().Should().BeFalse("a head edits the draft track of their unit");
        withoutLevels.Hr.Kinds().Should().Equal("DraftAwaitsApproval");
        withoutLevels.Hr["group"]!.GetValue<string>().Should().Be("DraftsToApprove");
        withoutLevels.Hr.FirstReason()["details"]!["levelCount"]!.GetValue<int>().Should().Be(0);
        withoutLevels.Hr.FirstReason()["occurredAt"].Should().NotBeNull("a draft is dated by its creation");
        oneLevel.Head.Kinds().Should().Equal("TrackWithOneLevel", "TrackWithoutPositions");
        oneLevel.Head.FirstReason()["details"]!["levelCount"]!.GetValue<int>().Should().Be(1);
        EntryIds(oneLevel.Hr.FirstReason()).Should().Equal(first.Id);
        oneLevelLinked.Head.Kinds().Should().Equal("TrackWithOneLevel");
        EntryIds(oneLevelLinked.Hr.FirstReason()).Should().BeEmpty("the one level has a position now");
        gap.Head.Kinds().Should().Equal("LevelWithoutPosition");
        EntryIds(gap.Head.FirstReason()).Should().Equal(second.Id);
        EntryIds(gap.Hr.FirstReason()).Should().Equal(second.Id);
        readyHead.Should().BeNull();
        readyHr.Kinds().Should().Equal("DraftAwaitsApproval");
        EntryIds(readyHr.FirstReason()).Should().BeEmpty();
        linked.Json!["id"]!.GetValue<Guid>().Should().Be(first.Id);
    }

    [Fact]
    public async Task Ac132_ForAnActiveTrack_TheHrIsAskedToLinkPositions_AndTheHeadIsOnlyToldWhomToAsk()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, (await org.Admin.MakeDirectionAsync()).Id);
        await org.Admin.MakeLevelAsync(track.Id);
        var last = await org.Admin.MakeLevelAsync(track.Id);
        (await org.Admin.ActivateAsync(track, org.Team.Id)).Expect(HttpStatusCode.OK);
        var levels = (await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray();
        var lastLevel = levels.Single(level => level!["id"]!.GetValue<Guid>() == last.Id)!;
        foreach (var position in lastLevel["positions"]!.AsArray())
        {
            var current = (await org.Admin.GetAsync($"/api/v1/positions/{position!["id"]!.GetValue<Guid>()}")).Expect(HttpStatusCode.OK);
            (await org.Admin.PostAsync($"/api/v1/positions/{current.Id}/deactivate", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
        }

        var items = await TrackItemsAsync(org, track);

        items.Head.Kinds().Should().Equal("LevelWithoutPosition");
        items.Head["isReference"]!.GetValue<bool>().Should().BeTrue("a head cannot edit an active track");
        items.Head["group"]!.GetValue<string>().Should().Be("PositionLinksInfo");
        items.Head.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.Hr.Employee, "the head is told whom to ask");
        EntryIds(items.Head.FirstReason()).Should().Equal(last.Id);
        items.Hr.Kinds().Should().Equal("LevelWithoutPosition");
        items.Hr["isReference"]!.GetValue<bool>().Should().BeFalse();
        items.Hr["group"]!.GetValue<string>().Should().Be("PositionsToLink");
        items.Hr.FirstReason()["details"]!["person"].Should().BeNull();
    }

    [Fact]
    public async Task Ac122_Ac151_APositionNoActiveTrackStandsFor_IsTheHeadsToLinkOnlyWhenATrackTheyMayEditCanTakeIt_AndItAbsorbsTheHeadsOwnNoTrackNote()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var positionId = (await org.Admin.GetEmployeeAsync(org.TeamHead.Employee)).Json!["positionId"]!.GetValue<Guid>();

        var noTrack = await org.TeamHead.Client.ItemsAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, (await org.Admin.MakeDirectionAsync()).Id);
        var draftTrack = await org.TeamHead.Client.ItemsAsync();
        var level = await org.Admin.MakeLevelAsync(track.Id);
        var availableBefore = AvailableIds((await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/available-positions")).Expect(HttpStatusCode.OK));
        (await org.Admin.PostAsync($"/api/v1/career/levels/{level.Id}/positions", new { positionId }, level.ETag)).Expect(HttpStatusCode.OK);
        var availableAfter = AvailableIds((await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/available-positions")).Expect(HttpStatusCode.OK));
        var linkedToDraft = await org.TeamHead.Client.ItemsAsync();
        (await org.Admin.ActivateAsync(track, org.Team.Id)).Expect(HttpStatusCode.OK);
        var linkedToActive = await org.TeamHead.Client.ItemsAsync();

        availableBefore.Should().Contain(positionId);
        availableAfter.Should().NotContain(positionId, "a position is linked to one level of a track at most");

        var reference = noTrack.ItemFor(positionId);
        reference.Kinds().Should().Equal("PositionWithoutTrack");
        reference["isReference"]!.GetValue<bool>().Should().BeTrue("no track exists that the head may edit and that could take the position");
        reference["group"]!.GetValue<string>().Should().Be("PositionLinksInfo");
        reference.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.Hr.Employee);
        reference.FirstReason()["details"]!["employeeCount"]!.GetValue<int>().Should().BeGreaterThan(0);
        noTrack.ItemOf(org.TeamHead.Employee).Should().BeNull("the finding about the position absorbs the head's own note about it");
        var action = draftTrack.ItemFor(positionId);
        action["isReference"]!.GetValue<bool>().Should().BeFalse();
        action["group"]!.GetValue<string>().Should().Be("PositionsToLink");
        action.FirstReason()["details"]!["person"].Should().BeNull();
        linkedToDraft.ItemOf(positionId).Should().NotBeNull("a draft track stands for nothing yet");
        linkedToActive.ItemOf(positionId).Should().BeNull();
        linkedToActive.ItemOf(org.TeamHead.Employee).Should().BeNull("the head's position has a track now");
    }

    [Fact]
    public async Task Ac151_Ac152_AnEmployeeWhosePositionHasNoActiveTrack_IsToldForReference_UntilAnActiveTrackStandsForIt()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var employee = await org.Admin.CreateEmployeeAsync(org.Team.Id, position: Scenarios.Unique("Должность"));
        var positionId = (await org.Admin.GetEmployeeAsync(employee.Id)).Json!["positionId"]!.GetValue<Guid>();
        var client = await (await org.Admin.CreateUserAsync(employeeId: employee.Id)).SignInAsync(fixture.Host);

        var before = await client.ItemsAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, (await org.Admin.MakeDirectionAsync()).Id);
        var level = await org.Admin.LinkToNewPositionAsync(await org.Admin.MakeLevelAsync(track.Id), org.Team.Id);
        (await org.Admin.PostAsync($"/api/v1/career/levels/{level.Id}/positions", new { positionId }, level.ETag)).Expect(HttpStatusCode.OK);
        var draft = await client.ItemsAsync();
        (await org.Admin.ActivateAsync(track, org.Team.Id)).Expect(HttpStatusCode.OK);
        var after = await client.ItemsAsync();

        var note = before.ItemFor(employee.Id);
        note.Kinds().Should().Equal("NoTrackForPosition");
        note["isReference"]!.GetValue<bool>().Should().BeTrue();
        note["group"]!.GetValue<string>().Should().Be("NoTrack");
        note["object"]!["type"]!.GetValue<string>().Should().Be("Employee");
        (await client.CountAsync()).Should().Be(0, "a note for reference is not an expected action");
        draft.ItemOf(employee.Id).Should().NotBeNull("a draft track stands for nothing yet");
        after.ItemOf(employee.Id).Should().BeNull();
        (await client.DeskAsync())["groups"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Ac25_Ac26_Ac161_AHeadWithoutAPublishedMatrix_IsAskedToBuildOne_AndTheHrIsToldWhoHasToWhile_AVersionInWorkSilencesBoth()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeActiveTrackAsync(org.Department.Id, direction.Id);

        var needed = await org.TeamHead.Client.ItemsAsync();
        var told = await org.Hr.Client.ItemsAsync();
        (await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id, directionId = direction.Id, trackId = track.Id })).Expect(HttpStatusCode.Created);
        var started = await org.TeamHead.Client.ItemsAsync();
        var hrAfter = await org.Hr.Client.ItemsAsync();

        var action = needed.ItemFor(org.Team.Id);
        action.Kinds().Should().Equal("UnitWithoutMatrix");
        action["group"]!.GetValue<string>().Should().Be("MatricesToBuild");
        action["isReference"]!.GetValue<bool>().Should().BeFalse();
        action.FirstReason()["details"]!["entries"]!.AsArray().Select(entry => entry!["id"]!.GetValue<Guid>()).Should().Equal(track.Id);
        var reference = told.ItemFor(org.Team.Id);
        reference.Kinds().Should().Equal("ResponsibleUnitWithoutMatrix");
        reference["isReference"]!.GetValue<bool>().Should().BeTrue();
        reference.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.TeamHead.Employee);
        started.ItemOf(org.Team.Id).Should().BeNull();
        hrAfter.ItemOf(org.Team.Id).Should().BeNull();
        needed.ItemOf(org.Department.Id).Should().BeNull("the head of the team answers for the team alone");
    }

    private static async Task<(JsonNode Head, JsonNode Hr)> TrackItemsAsync(CareerOrg org, ApiResponse track) =>
        ((await org.TeamHead.Client.ItemsAsync()).ItemFor(track.Id), (await org.Hr.Client.ItemsAsync()).ItemFor(track.Id));

    private static Guid[] AvailableIds(ApiResponse page) => [.. page.Json!["items"]!.AsArray().Select(position => position!["id"]!.GetValue<Guid>())];

    private static Guid[] EntryIds(JsonNode reason) =>
        [.. (reason["details"]?["entries"]?.AsArray() ?? []).Select(entry => entry!["id"]!.GetValue<Guid>())];
}
