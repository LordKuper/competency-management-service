using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The links between the levels of a track and the positions that stand for them: who may change them, which positions a level may take,
/// what a link keeps a track or a matrix version from doing, and what the reading methods say.
/// </summary>
public sealed class LevelPositionTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string LinkedElsewhere = "Должность уже связана с другим уровнем трека";
    private const string UnavailablePosition = "Должность неактивна или недоступна подразделению трека";

    [Fact]
    public async Task Ac91_Ac93_LinksOfADraftTrack_AreChangedByTheHeadOfTheUnitTheHrAndTheAdministrator_WithTheVersionOfTheLevel_AndJournaled()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (_, level) = await DraftTrackWithLevelAsync(org);
        var positions = new[] { await PositionAsync(org, org.Team.Id), await PositionAsync(org, org.Team.Id), await PositionAsync(org, org.Team.Id), await PositionAsync(org, org.Team.Id) };

        var byPlain = await LinkAsync(org.Plain.Client, level, positions[0]);
        var bySiblingHead = await LinkAsync(org.SiblingHead.Client, level, positions[0]);
        var bySiblingHr = await LinkAsync(org.SiblingHr.Client, level, positions[0]);
        var byHead = (await LinkAsync(org.TeamHead.Client, level, positions[0])).Expect(HttpStatusCode.OK);
        var byHr = (await LinkAsync(org.Hr.Client, byHead, positions[1])).Expect(HttpStatusCode.OK);
        var byAdmin = (await LinkAsync(org.Admin, byHr, positions[2])).Expect(HttpStatusCode.OK);
        var stale = await LinkAsync(org.Admin, level, positions[3]);
        var withoutTag = await org.Admin.PostAsync($"/api/v1/career/levels/{level.Id}/positions", new { positionId = positions[3] });

        byPlain.Status.Should().Be(HttpStatusCode.NotFound, "a draft track is read by the people of its unit alone");
        bySiblingHead.Status.Should().Be(HttpStatusCode.NotFound);
        bySiblingHr.Status.Should().Be(HttpStatusCode.NotFound);
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        withoutTag.Status.Should().Be(HttpStatusCode.PreconditionRequired);
        byAdmin.ETag.Should().NotBe(level.ETag, "a link changes the level's version");
        var journal = await org.Admin.AuditOfRequestAsync(byHead.RequestId!);
        journal.Actions().Should().Equal("CareerLevelPosition.Created");
        journal[0]!["entityId"]!.GetValue<string>().Should().Be($"{level.Id},{positions[0]}");

        var unlinked = (await org.TeamHead.Client.DeleteAsync($"/api/v1/career/levels/{level.Id}/positions/{positions[0]}", byAdmin.ETag)).Expect(HttpStatusCode.OK);
        (await org.Admin.AuditOfRequestAsync(unlinked.RequestId!)).Actions().Should().Equal("CareerLevelPosition.Deleted");
        (await org.TeamHead.Client.DeleteAsync($"/api/v1/career/levels/{level.Id}/positions/{positions[0]}", unlinked.ETag)).Status
            .Should().Be(HttpStatusCode.NotFound, "the link is gone");
    }

    [Fact]
    public async Task Ac91_AnActiveTrack_HasItsLinksChangedByTheHrAndTheAdministratorOnly_AndItsHeadMayReadThem()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (track, level) = await DraftTrackWithLevelAsync(org);
        var linked = await org.Admin.LinkToNewPositionAsync(level, org.Team.Id);
        (await org.Admin.ActivateAsync(track, org.Team.Id)).Expect(HttpStatusCode.OK);
        var fresh = (await org.Admin.GetAsync($"/api/v1/career/levels/{linked.Id}")).Expect(HttpStatusCode.OK);
        var position = await PositionAsync(org, org.Team.Id);

        var byHead = await LinkAsync(org.TeamHead.Client, fresh, position);
        var byPlain = await LinkAsync(org.Plain.Client, fresh, position);
        var byHr = await LinkAsync(org.Hr.Client, fresh, position);

        byHead.Status.Should().Be(HttpStatusCode.Forbidden);
        byPlain.Status.Should().Be(HttpStatusCode.Forbidden);
        byHr.Status.Should().Be(HttpStatusCode.OK);
        var read = (await org.TeamHead.Client.GetAsync($"/api/v1/career/tracks/{track.Id}/positions")).Expect(HttpStatusCode.OK).Json!;
        read["canEdit"]!.GetValue<bool>().Should().BeFalse();
        (await org.Hr.Client.GetAsync($"/api/v1/career/tracks/{track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["canEdit"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Ac88_Ac90_ALevelTakesAnActivePositionOfTheTrackUnitOrAUnitAboveOrBelowIt_OnceInTheTrack_AndRepeatingALinkChangesNothing()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (track, first) = await DraftTrackWithLevelAsync(org);
        var second = await org.Admin.MakeLevelAsync(track.Id);
        var child = await org.Admin.CreateUnitAsync(org.Team.Id);
        var inTeam = await PositionAsync(org, org.Team.Id);
        var inParent = await PositionAsync(org, org.Department.Id);
        var inChild = await PositionAsync(org, child.Id);
        var inSibling = await PositionAsync(org, org.Sibling.Id);
        var inactive = await PositionAsync(org, org.Team.Id);
        var current = (await org.Admin.GetAsync($"/api/v1/positions/{inactive}")).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/positions/{inactive}/deactivate", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);

        var linked = (await LinkAsync(org.Admin, first, inTeam)).Expect(HttpStatusCode.OK);
        var repeated = (await LinkAsync(org.Admin, linked, inTeam)).Expect(HttpStatusCode.OK);
        var other = await LinkAsync(org.Admin, second, inTeam);
        var above = await LinkAsync(org.Admin, repeated, inParent);
        var below = await LinkAsync(org.Admin, above.Expect(HttpStatusCode.OK), inChild);
        var beside = await LinkAsync(org.Admin, below.Expect(HttpStatusCode.OK), inSibling);
        var notActive = await LinkAsync(org.Admin, below, inactive);
        var unknown = await LinkAsync(org.Admin, below, Guid.NewGuid());

        repeated.ETag.Should().Be(linked.ETag, "linking a position again changes nothing");
        other.Status.Should().Be(HttpStatusCode.Conflict);
        other.Json!["detail"]!.GetValue<string>().Should().Be(LinkedElsewhere);
        beside.Status.Should().Be(HttpStatusCode.BadRequest);
        beside.FieldErrors("positionId").Should().Equal(UnavailablePosition);
        notActive.FieldErrors("positionId").Should().Equal(UnavailablePosition);
        unknown.FieldErrors("positionId").Should().Equal(UnavailablePosition);
    }

    [Fact]
    public async Task Ac90_APositionWithAMatrixOfItsOwn_CannotBeLinked_AndAPositionAlreadyLinked_CannotGetAMatrix()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (track, level) = await DraftTrackWithLevelAsync(org);
        var withMatrix = await PositionAsync(org, org.Team.Id);
        var linkedPosition = await PositionAsync(org, org.Team.Id);
        (await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id, positionId = withMatrix })).Expect(HttpStatusCode.Created);
        var linked = (await LinkAsync(org.Admin, level, linkedPosition)).Expect(HttpStatusCode.OK);

        var refusedLink = await LinkAsync(org.Admin, linked, withMatrix);
        var refusedMatrix = await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id, positionId = linkedPosition });
        var availableInTrack = (await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/available-positions?pageSize=200")).Expect(HttpStatusCode.OK);
        var availableForMatrix = (await org.TeamHead.Client.GetAsync($"/api/v1/career/org-units/{org.Team.Id}/matrix-positions?pageSize=200")).Expect(HttpStatusCode.OK);

        refusedLink.Status.Should().Be(HttpStatusCode.Conflict);
        refusedLink.Json!["detail"]!.GetValue<string>().Should().Contain("матрица должности");
        refusedMatrix.Status.Should().Be(HttpStatusCode.Conflict);
        refusedMatrix.Json!["detail"]!.GetValue<string>().Should().Contain("связана с уровнем трека");
        availableInTrack.ItemIds().Should().NotContain([withMatrix.ToString(), linkedPosition.ToString()], "one has a matrix of its own and the other is linked already");
        var forMatrix = availableForMatrix.Json!["items"]!.AsArray();
        forMatrix.Single(item => item!["id"]!.GetValue<Guid>() == linkedPosition)!["isLinked"]!.GetValue<bool>().Should().BeTrue();
        forMatrix.Single(item => item!["id"]!.GetValue<Guid>() == withMatrix)!["isLinked"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task Ac97_Ac100_Ac102_ATrackWithoutLevelsOrWithALevelWithoutAPosition_CannotBecomeActive_AndTheRefusalNamesFiveAndCountsTheRest()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, (await org.Admin.MakeDirectionAsync()).Id);
        var path = $"/api/v1/career/tracks/{track.Id}";

        var noLevels = await org.Admin.PostAsync($"{path}/approve", ifMatch: track.ETag);
        var levels = new List<ApiResponse>();
        for (var count = 0; count < 7; count++)
        {
            levels.Add(await org.Admin.MakeLevelAsync(track.Id));
        }

        var manyGaps = await org.Admin.PostAsync($"{path}/approve", ifMatch: (await org.Admin.GetAsync(path)).ETag);
        foreach (var level in levels.Take(6))
        {
            await org.Admin.LinkToNewPositionAsync((await org.Admin.GetAsync($"/api/v1/career/levels/{level.Id}")).Expect(HttpStatusCode.OK), org.Team.Id);
        }

        var oneGap = await org.Admin.PostAsync($"{path}/approve", ifMatch: (await org.Admin.GetAsync(path)).ETag);

        noLevels.Status.Should().Be(HttpStatusCode.Conflict);
        noLevels.Json!["blockers"]!.AsArray().Select(blocker => $"{blocker!["kind"]}/{blocker["reason"]}/{blocker["name"]}").Should().Equal($"Track/NoLevels/{track.Json!["name"]!.GetValue<string>()}");
        manyGaps.Status.Should().Be(HttpStatusCode.Conflict);
        var named = manyGaps.Json!["blockers"]!.AsArray();
        named.Should().HaveCount(5);
        named.Select(blocker => blocker!["reason"]!.GetValue<string>()).Should().OnlyContain(reason => reason == "NoActivePosition");
        named.Select(blocker => blocker!["kind"]!.GetValue<string>()).Should().OnlyContain(kind => kind == "Level");
        named.Select(blocker => blocker!["context"]!.GetValue<string>()).Should().OnlyContain(context => context == track.Json["name"]!.GetValue<string>());
        manyGaps.Json["moreCount"]!.GetValue<int>().Should().Be(2);
        oneGap.Json!["blockers"]!.AsArray().Select(blocker => blocker!["id"]!.GetValue<Guid>()).Should().Equal(levels[6].Id);
        (await org.Admin.GetAsync(path)).Json!["status"]!.GetValue<string>().Should().Be("Draft");
        (await org.Admin.AuditOfRequestAsync(oneGap.RequestId!)).Should().BeEmpty("a refused publication changes and journals nothing");
    }

    [Fact]
    public async Task Ac97_ABackToWorkTrack_NeedsALinkedActivePositionOnEveryLevelToo()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, (await org.Admin.MakeDirectionAsync()).Id);
        await org.Admin.MakeLevelAsync(track.Id);
        (await org.Admin.ActivateAsync(track, org.Team.Id)).Expect(HttpStatusCode.OK);
        var path = $"/api/v1/career/tracks/{track.Id}";
        var deactivated = (await org.Admin.PostAsync($"{path}/deactivate", ifMatch: (await org.Admin.GetAsync(path)).ETag)).Expect(HttpStatusCode.OK);
        foreach (var position in (await org.Admin.GetAsync($"{path}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray().SelectMany(level => level!["positions"]!.AsArray()))
        {
            var current = (await org.Admin.GetAsync($"/api/v1/positions/{position!["id"]!.GetValue<Guid>()}")).Expect(HttpStatusCode.OK);
            (await org.Admin.PostAsync($"/api/v1/positions/{current.Id}/deactivate", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
        }

        var refused = await org.Admin.PostAsync($"{path}/activate", ifMatch: deactivated.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["blockers"]!.AsArray().Select(blocker => blocker!["reason"]!.GetValue<string>()).Should().Equal("NoActivePosition");
        (await org.Admin.GetAsync(path)).Json!["status"]!.GetValue<string>().Should().Be("Inactive");
    }

    [Fact]
    public async Task Ac98_Ac95_Ac96_TheLastActivePositionOfALevelOfAnActiveTrack_StaysLinked_WhileInactiveOnesNeverCount_AndDeactivatingAPositionIsNotBlocked()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (track, level) = await DraftTrackWithLevelAsync(org);
        var first = await PositionAsync(org, org.Team.Id);
        var second = await PositionAsync(org, org.Team.Id);
        var third = await PositionAsync(org, org.Team.Id);
        var onlyLevel = (await LinkAsync(org.Admin, level, first)).Expect(HttpStatusCode.OK);
        onlyLevel = (await LinkAsync(org.Admin, onlyLevel, second)).Expect(HttpStatusCode.OK);
        var draftUnlink = (await org.Admin.DeleteAsync($"/api/v1/career/levels/{level.Id}/positions/{second}", onlyLevel.ETag)).Expect(HttpStatusCode.OK);
        var draftLastUnlink = (await org.Admin.DeleteAsync($"/api/v1/career/levels/{level.Id}/positions/{first}", draftUnlink.ETag)).Expect(HttpStatusCode.OK);
        (await LinkAsync(org.Admin, draftLastUnlink, first)).Expect(HttpStatusCode.OK);
        await ApproveAsync(org.Admin, track);
        var active = (await org.Admin.GetAsync($"/api/v1/career/levels/{level.Id}")).Expect(HttpStatusCode.OK);
        var secondRead = (await org.Admin.GetAsync($"/api/v1/positions/{first}")).Expect(HttpStatusCode.OK);

        var lastButStale = await org.Admin.DeleteAsync($"/api/v1/career/levels/{level.Id}/positions/{first}", "\"1\"");
        var withInactive = (await LinkAsync(org.Admin, active, second)).Expect(HttpStatusCode.OK);
        var currentSecond = (await org.Admin.GetAsync($"/api/v1/positions/{second}")).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/positions/{second}/deactivate", ifMatch: currentSecond.ETag)).Expect(HttpStatusCode.OK);
        var removeInactive = (await org.Admin.DeleteAsync($"/api/v1/career/levels/{level.Id}/positions/{second}", withInactive.ETag)).Expect(HttpStatusCode.OK);
        var withThird = (await LinkAsync(org.Admin, removeInactive, third)).Expect(HttpStatusCode.OK);
        var removeFirst = (await org.Admin.DeleteAsync($"/api/v1/career/levels/{level.Id}/positions/{first}", withThird.ETag)).Expect(HttpStatusCode.OK);
        var lastActive = await org.Admin.DeleteAsync($"/api/v1/career/levels/{level.Id}/positions/{third}", removeFirst.ETag);

        draftUnlink.Json!["id"]!.GetValue<Guid>().Should().Be(level.Id);
        lastButStale.Status.Should().Be(HttpStatusCode.Conflict, "the refusal is decided before the version is looked at");
        lastButStale.Json!["blockers"]!.AsArray().Select(blocker => blocker!["reason"]!.GetValue<string>()).Should().Equal("NoActivePosition");
        lastActive.Status.Should().Be(HttpStatusCode.Conflict);
        secondRead.Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        var links = (await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray()
            .Single(item => item!["id"]!.GetValue<Guid>() == level.Id)!;
        links["positions"]!.AsArray().Select(position => position!["id"]!.GetValue<Guid>()).Should().Equal(third);
        links["positions"]![0]!["canUnlink"]!.GetValue<bool>().Should().BeFalse("it is the last active position of an active track");
        links["hasActivePosition"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Ac95_Ac96_ADeactivatedLinkedPosition_StaysLinked_AndTheLevelCountsAsWithoutAPosition()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (track, level) = await DraftTrackWithLevelAsync(org);
        var position = await PositionAsync(org, org.Team.Id);
        (await LinkAsync(org.Admin, level, position)).Expect(HttpStatusCode.OK);
        await ApproveAsync(org.Admin, track);
        var current = (await org.Admin.GetAsync($"/api/v1/positions/{position}")).Expect(HttpStatusCode.OK);

        (await org.Admin.PostAsync($"/api/v1/positions/{position}/deactivate", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);

        var read = (await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray().Single()!;
        read["hasActivePosition"]!.GetValue<bool>().Should().BeFalse();
        read["positions"]!.AsArray().Select(item => (item!["id"]!.GetValue<Guid>(), item["isActive"]!.GetValue<bool>())).Should().Equal((position, false));
    }

    [Fact]
    public async Task Ac87_ATracksPositions_ListEachLevelWithItsPositionsAndHeadcount_AndTheAvailableOnesExcludeLinkedAndSearchByName()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (track, level) = await DraftTrackWithLevelAsync(org);
        var other = await org.Admin.MakeLevelAsync(track.Id);
        var name = Scenarios.Unique("Искомая");
        var wanted = (await org.Admin.PostAsync($"/api/v1/org-units/{org.Team.Id}/positions/quick-add", new { name })).Expect(HttpStatusCode.OK).Id;
        var occupied = await org.Admin.CreateEmployeeAsync(org.Team.Id, position: Scenarios.Unique("Занятая"));
        var occupiedId = (await org.Admin.GetEmployeeAsync(occupied.Id)).Json!["positionId"]!.GetValue<Guid>();
        var occupiedName = (await org.Admin.GetEmployeeAsync(occupied.Id)).Json!["position"]!.GetValue<string>();
        await org.Admin.CreateEmployeeAsync(org.Team.Id, position: occupiedName);
        await org.Admin.CreateEmployeeAsync(org.Sibling.Id, position: occupiedName);
        (await LinkAsync(org.Admin, level, occupiedId)).Expect(HttpStatusCode.OK);

        var linked = (await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/positions")).Expect(HttpStatusCode.OK).Json!;
        var found = (await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/available-positions?q={Uri.EscapeDataString(name[..9])}")).Expect(HttpStatusCode.OK);
        var plain = await org.Plain.Client.GetAsync($"/api/v1/career/tracks/{track.Id}/positions");
        var unknown = await org.Admin.GetAsync($"/api/v1/career/tracks/{Guid.NewGuid()}/positions");
        var badPage = await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}/available-positions?page=0");

        var levels = linked["levels"]!.AsArray();
        levels.Select(item => item!["id"]!.GetValue<Guid>()).Should().Equal(level.Id, other.Id);
        var first = levels[0]!["positions"]!.AsArray().Single()!;
        first["id"]!.GetValue<Guid>().Should().Be(occupiedId);
        first["employeeCount"]!.GetValue<int>().Should().Be(2, "only the employees of the track's unit and below count, not those of the sibling unit");
        levels[1]!["positions"]!.AsArray().Should().BeEmpty();
        levels[1]!["hasActivePosition"]!.GetValue<bool>().Should().BeFalse();
        found.ItemIds().Should().Equal(wanted.ToString());
        found.Json!["items"]![0]!["employeeCount"]!.GetValue<int>().Should().Be(0);
        plain.Status.Should().Be(HttpStatusCode.NotFound, "a draft track is invisible to a user outside its unit");
        unknown.Status.Should().Be(HttpStatusCode.NotFound);
        badPage.Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ac87_DeletingALevelOrATrack_TakesItsLinksWithIt()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (track, level) = await DraftTrackWithLevelAsync(org);
        var linked = (await LinkAsync(org.Admin, level, await PositionAsync(org, org.Team.Id))).Expect(HttpStatusCode.OK);
        var second = await org.Admin.MakeLevelAsync(track.Id);
        var secondLinked = (await LinkAsync(org.Admin, second, await PositionAsync(org, org.Team.Id))).Expect(HttpStatusCode.OK);

        (await org.Admin.DeleteAsync($"/api/v1/career/levels/{second.Id}", secondLinked.ETag)).Expect(HttpStatusCode.NoContent);
        var afterLevel = await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM career_level_positions WHERE track_id = '{track.Id}'");
        var trackNow = (await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}")).Expect(HttpStatusCode.OK);
        (await org.Admin.DeleteAsync($"/api/v1/career/levels/{level.Id}", linked.ETag)).Expect(HttpStatusCode.NoContent);
        (await org.Admin.DeleteAsync($"/api/v1/career/tracks/{track.Id}", (await org.Admin.GetAsync($"/api/v1/career/tracks/{track.Id}")).ETag)).Expect(HttpStatusCode.NoContent);

        afterLevel.Should().Be(1);
        trackNow.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM career_level_positions WHERE track_id = '{track.Id}'")).Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ac103_Ac99_AVersionWhoseLevelLostItsPosition_CannotBeHandedOn_AndCannotBePublished(bool afterHandOn)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var positionOfJunior = (await org.Admin.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray()
            .Single(level => level!["id"]!.GetValue<Guid>() == kit.Junior.Id)!["positions"]![0]!["id"]!.GetValue<Guid>();
        if (afterHandOn)
        {
            (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
            (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        }

        var current = (await org.Admin.GetAsync($"/api/v1/positions/{positionOfJunior}")).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/positions/{positionOfJunior}/deactivate", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
        var completeness = (await kit.Author.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/completeness")).Expect(HttpStatusCode.OK);
        var refused = afterHandOn
            ? await kit.TransitAsync(org.Hr.Client, "approve")
            : await kit.TransitAsync(org.TeamHead.Client, "submit");

        completeness.Json!["problems"]!.AsArray().Select(problem => problem!["code"]!.GetValue<string>()).Should().Equal("LevelWithoutActivePosition");
        completeness.Json["problems"]![0]!["levelId"]!.GetValue<Guid>().Should().Be(kit.Junior.Id);
        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        (await kit.StatusAsync()).Should().Be(afterHandOn ? "OnReview" : "Draft");
    }

    private static async Task ApproveAsync(ApiClient client, ApiResponse track)
    {
        var path = $"/api/v1/career/tracks/{track.Id}";
        var current = (await client.GetAsync(path)).Expect(HttpStatusCode.OK);
        var tag = (await client.GetAsync($"{path}/publication")).Expect(HttpStatusCode.OK).Json!["cascadeTag"]!.GetValue<string>();
        (await client.PostAsync($"{path}/approve?cascade={Uri.EscapeDataString(tag)}", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
    }

    private static async Task<(ApiResponse Track, ApiResponse Level)> DraftTrackWithLevelAsync(CareerOrg org)
    {
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, (await org.Admin.MakeDirectionAsync()).Id);
        return (track, await org.Admin.MakeLevelAsync(track.Id));
    }

    private static async Task<Guid> PositionAsync(CareerOrg org, Guid unitId) => await org.Admin.PositionAsync(unitId, Scenarios.Unique("Должность"));

    private static Task<ApiResponse> LinkAsync(ApiClient client, ApiResponse level, Guid positionId) =>
        client.PostAsync($"/api/v1/career/levels/{level.Id}/positions", new { positionId }, level.ETag);
}
