using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Career tracks belong to a unit and are kept by its responsible HR and by administrators, the unit's head proposes drafts of them, and a
/// level is a part of its track: whoever may change the track creates, renames, deletes and orders its levels, and approving a track
/// approves its draft levels. A draft track is read by its author, the head and the responsible HR of its unit and administrators.
/// </summary>
public sealed class TrackAndLevelTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string UnavailableMessage = "Запись не найдена или недоступна";

    [Fact]
    public async Task Ac9_NewTrack_IsADraftFromAdministratorsTheUnitsResponsibleHrAndTheUnitsHead_AndRefusedToTheRest()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();

        var byAdmin = await CreateTrackAsync(org.Admin, org.Team.Id, direction.Id);
        var byHr = await CreateTrackAsync(org.Hr.Client, org.Team.Id, direction.Id);
        var byHead = await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id);
        var byHrOfAnotherUnit = await CreateTrackAsync(org.SiblingHr.Client, org.Team.Id, direction.Id);
        var byHeadOfAnotherUnit = await CreateTrackAsync(org.TeamHead.Client, org.Sibling.Id, direction.Id);
        var byHeadAbove = await CreateTrackAsync(org.DepartmentHead.Client, org.Team.Id, direction.Id);
        var byPlain = await CreateTrackAsync(org.Plain.Client, org.Team.Id, direction.Id);

        byAdmin.Expect(HttpStatusCode.Created).Json!["status"]!.GetValue<string>().Should().Be("Draft", "a track is approved once it has levels with positions");
        byHr.Expect(HttpStatusCode.Created).Json!["status"]!.GetValue<string>().Should().Be("Draft");
        byHead.Expect(HttpStatusCode.Created).Json!["status"]!.GetValue<string>().Should().Be("Draft");
        byHead.Json!["ownerOrgUnitId"]!.GetValue<Guid>().Should().Be(org.Team.Id);
        byHead.Json["ownerOrgUnitName"]!.GetValue<string>().Should().Be(org.Team.Json!["name"]!.GetValue<string>());
        byHrOfAnotherUnit.Status.Should().Be(HttpStatusCode.Forbidden, "the HR of a unit with an assignment of its own does not answer for this one");
        byHeadOfAnotherUnit.Status.Should().Be(HttpStatusCode.Forbidden);
        byHeadAbove.Status.Should().Be(HttpStatusCode.Forbidden, "a head proposes tracks of their own unit only");
        byPlain.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ac9_NewTrack_NeedsAnActiveUnitAndAUsableDirection()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var inactiveDirection = await org.Admin.MakeDirectionAsync();
        (await org.Admin.PostAsync($"/api/v1/career/directions/{inactiveDirection.Id}/deactivate", ifMatch: inactiveDirection.ETag)).Expect(HttpStatusCode.OK);
        var othersDraft = (await org.SiblingHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Чужое") })).Expect(HttpStatusCode.Created);
        var ownDraft = (await org.TeamHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Своё") })).Expect(HttpStatusCode.Created);
        var closed = await org.Admin.CreateUnitAsync(org.Root.Id);
        (await org.Admin.PostAsync($"/api/v1/org-units/{closed.Id}/deactivate", ifMatch: closed.ETag)).Expect(HttpStatusCode.OK);

        var unknownUnit = await CreateTrackAsync(org.Admin, Guid.NewGuid(), direction.Id);
        var inactiveUnit = await CreateTrackAsync(org.Admin, closed.Id, direction.Id);
        var unknownDirection = await CreateTrackAsync(org.Admin, org.Team.Id, Guid.NewGuid());
        var inactiveDirectionTrack = await CreateTrackAsync(org.Admin, org.Team.Id, inactiveDirection.Id);
        var othersDraftTrack = await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, othersDraft.Id);
        var ownDraftTrack = await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, ownDraft.Id);

        unknownUnit.FieldErrors("ownerOrgUnitId").Should().Equal(UnavailableMessage);
        inactiveUnit.FieldErrors("ownerOrgUnitId").Should().Equal(UnavailableMessage);
        unknownDirection.FieldErrors("directionId").Should().Equal(UnavailableMessage);
        inactiveDirectionTrack.FieldErrors("directionId").Should().Equal(UnavailableMessage);
        othersDraftTrack.FieldErrors("directionId").Should().Equal([UnavailableMessage], "a draft is usable by its author only");
        ownDraftTrack.Status.Should().Be(HttpStatusCode.Created, ownDraftTrack.Body);
    }

    [Fact]
    public async Task Ac9_ADraftTrack_IsReadByItsAuthorTheHeadAndTheResponsibleHrOfItsUnitAndAdministratorsOnly()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var draft = (await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id)).Expect(HttpStatusCode.Created);
        var name = draft.Json!["name"]!.GetValue<string>();

        foreach (var reader in new[] { org.TeamHead, org.Hr })
        {
            (await reader.Client.GetAsync($"/api/v1/career/tracks/{draft.Id}")).Status.Should().Be(HttpStatusCode.OK, reader.User.Email);
            (await TrackNamesAsync(reader.Client, name)).Should().Equal(name);
        }

        (await org.Admin.GetAsync($"/api/v1/career/tracks/{draft.Id}")).Status.Should().Be(HttpStatusCode.OK);
        foreach (var stranger in new[] { org.SiblingHr, org.DepartmentHead, org.SiblingHead, org.Plain })
        {
            (await stranger.Client.GetAsync($"/api/v1/career/tracks/{draft.Id}")).Status.Should().Be(HttpStatusCode.NotFound, stranger.User.Email);
            (await TrackNamesAsync(stranger.Client, name)).Should().BeEmpty(stranger.User.Email);
        }
    }

    [Fact]
    public async Task Ac9_ActiveAndInactiveTracks_AreReadByEveryHr_AndInactiveOnesByNoHead()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var active = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var inactive = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        (await org.Admin.PostAsync($"/api/v1/career/tracks/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);

        (await org.Plain.Client.GetAsync($"/api/v1/career/tracks/{active.Id}")).Status.Should().Be(HttpStatusCode.OK);
        (await org.SiblingHr.Client.GetAsync($"/api/v1/career/tracks/{inactive.Id}")).Status.Should().Be(HttpStatusCode.OK, "every HR reads the inactive tracks of all units");
        (await org.TeamHead.Client.GetAsync($"/api/v1/career/tracks/{inactive.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
        (await org.Plain.Client.GetAsync($"/api/v1/career/tracks/{inactive.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac9_AHead_RenamesTheirUnitsDraftTrackUntilItIsApproved_AndKeepersRenameAnyTrackOfTheirUnits()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var draft = (await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id)).Expect(HttpStatusCode.Created);

        var byHead = await RenameAsync(org.TeamHead.Client, draft, Scenarios.Unique("Правка"));
        var byHrOfAnotherUnit = await RenameAsync(org.SiblingHr.Client, byHead, Scenarios.Unique("Чужая"));
        var byHeadAbove = await RenameAsync(org.DepartmentHead.Client, byHead, Scenarios.Unique("Чужая"));
        var byHr = await RenameAsync(org.Hr.Client, byHead, Scenarios.Unique("HR"));
        await org.Admin.MakeLevelAsync(draft.Id);
        await org.Admin.ActivateAsync(draft, org.Team.Id, org.Hr.Client);
        var approved = (await org.Admin.GetAsync($"/api/v1/career/tracks/{draft.Id}")).Expect(HttpStatusCode.OK);
        var afterApproval = await RenameAsync(org.TeamHead.Client, approved, Scenarios.Unique("Поздно"));
        var visibleToOtherHr = await RenameAsync(org.SiblingHr.Client, approved, Scenarios.Unique("Чужая"));

        byHead.Status.Should().Be(HttpStatusCode.OK, byHead.Body);
        byHrOfAnotherUnit.Status.Should().Be(HttpStatusCode.NotFound);
        byHeadAbove.Status.Should().Be(HttpStatusCode.NotFound);
        byHr.Status.Should().Be(HttpStatusCode.OK, byHr.Body);
        afterApproval.Status.Should().Be(HttpStatusCode.Forbidden);
        visibleToOtherHr.Status.Should().Be(HttpStatusCode.Forbidden, "an active track is visible to every HR but kept by the HR of its unit");
    }

    [Fact]
    public async Task Ac10_TrackName_IsUniqueWithinItsDirectionRegardlessOfCase_ButNotAcrossDirections()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var other = await org.Admin.MakeDirectionAsync();
        var name = Scenarios.Unique("Бэкенд");
        (await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id, name)).Expect(HttpStatusCode.Created);

        var sameDirection = await org.Admin.PostAsync("/api/v1/career/tracks", new { name = name.ToUpperInvariant(), ownerOrgUnitId = org.Sibling.Id, directionId = direction.Id });
        var otherDirection = await org.Admin.PostAsync("/api/v1/career/tracks", new { name, ownerOrgUnitId = org.Team.Id, directionId = other.Id });

        sameDirection.Status.Should().Be(HttpStatusCode.Conflict);
        sameDirection.FieldErrors("name").Should().Equal("Запись с таким названием уже есть");
        otherDirection.Status.Should().Be(HttpStatusCode.Created, otherDirection.Body);
    }

    [Fact]
    public async Task Ac10_Rename_ChangesTheNameAlone_TheDirectionAndTheUnitNeverChange()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);

        var renamed = (await org.Admin.PutAsync(
            $"/api/v1/career/tracks/{track.Id}",
            new { name = Scenarios.Unique("Новое"), ownerOrgUnitId = org.Sibling.Id, directionId = Guid.NewGuid() },
            track.ETag)).Expect(HttpStatusCode.OK);

        renamed.Json!["ownerOrgUnitId"]!.GetValue<Guid>().Should().Be(org.Team.Id);
        renamed.Json["directionId"]!.GetValue<Guid>().Should().Be(direction.Id);
    }

    [Fact]
    public async Task Ac11_NewLevel_TakesTheStatusOfItsTrack_AndGoesToTheEndOfIt()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var active = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var draft = (await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id)).Expect(HttpStatusCode.Created);

        var first = await org.Admin.MakeLevelAsync(active.Id);
        var second = await org.Hr.Client.MakeLevelAsync(active.Id);
        var ofDraft = await org.TeamHead.Client.MakeLevelAsync(draft.Id);

        first.Json!["position"]!.GetValue<int>().Should().Be(2, "the active track already has the level that made it ready");
        second.Json!["position"]!.GetValue<int>().Should().Be(3);
        second.Json["trackId"]!.GetValue<Guid>().Should().Be(active.Id);
        second.Json["directionId"]!.GetValue<Guid>().Should().Be(direction.Id);
        first.Json["status"]!.GetValue<string>().Should().Be("Active");
        ofDraft.Json!["status"]!.GetValue<string>().Should().Be("Draft");
    }

    [Fact]
    public async Task Ac11_NewLevel_IsCreatedByWhoeverMayChangeTheTrack_AndNeedsAUsableTrack()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var active = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var draft = (await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id)).Expect(HttpStatusCode.Created);
        var inactive = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        (await org.Admin.PostAsync($"/api/v1/career/tracks/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);

        var headOnActive = await NewLevelAsync(org.TeamHead.Client, active.Id);
        var hrOfAnotherUnit = await NewLevelAsync(org.SiblingHr.Client, active.Id);
        var plain = await NewLevelAsync(org.Plain.Client, active.Id);
        var headOnDraft = await NewLevelAsync(org.TeamHead.Client, draft.Id);
        var strangerOnDraft = await NewLevelAsync(org.SiblingHead.Client, draft.Id);
        var onInactive = await NewLevelAsync(org.Admin, inactive.Id);
        var onUnknown = await NewLevelAsync(org.Admin, Guid.NewGuid());

        headOnActive.Status.Should().Be(HttpStatusCode.Forbidden);
        hrOfAnotherUnit.Status.Should().Be(HttpStatusCode.Forbidden);
        plain.Status.Should().Be(HttpStatusCode.Forbidden);
        headOnDraft.Status.Should().Be(HttpStatusCode.Created, headOnDraft.Body);
        strangerOnDraft.FieldErrors("trackId").Should().Equal([UnavailableMessage], "a draft track the caller cannot read is not found");
        onInactive.FieldErrors("trackId").Should().Equal(UnavailableMessage);
        onUnknown.FieldErrors("trackId").Should().Equal(UnavailableMessage);
    }

    [Fact]
    public async Task Ac11_LevelName_IsUniqueWithinItsTrackRegardlessOfCase_AndRenamedByWhoeverMayChangeTheTrack()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var other = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var level = await org.Admin.MakeLevelAsync(track.Id, "Старший");

        var duplicate = await org.Admin.PostAsync("/api/v1/career/levels", new { name = "СТАРШИЙ", trackId = track.Id });
        var inOtherTrack = await org.Admin.PostAsync("/api/v1/career/levels", new { name = "Старший", trackId = other.Id });
        var renamed = await org.Hr.Client.PutAsync($"/api/v1/career/levels/{level.Id}", new { name = "Ведущий" }, level.ETag);
        var byHead = await org.TeamHead.Client.PutAsync($"/api/v1/career/levels/{level.Id}", new { name = "Чужой" }, renamed.ETag);

        duplicate.Status.Should().Be(HttpStatusCode.Conflict);
        duplicate.FieldErrors("name").Should().Equal("Запись с таким названием уже есть");
        inOtherTrack.Status.Should().Be(HttpStatusCode.Created);
        renamed.Status.Should().Be(HttpStatusCode.OK, renamed.Body);
        byHead.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ac11_ApprovingATrack_ApprovesItsDraftLevelsAndJournalsEach()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var draft = (await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id)).Expect(HttpStatusCode.Created);
        var junior = await org.TeamHead.Client.MakeLevelAsync(draft.Id);
        var senior = await org.TeamHead.Client.MakeLevelAsync(draft.Id);

        var approved = await org.Admin.ActivateAsync(draft, org.Team.Id, org.Hr.Client);

        (await org.Plain.Client.GetAsync($"/api/v1/career/levels/{junior.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Active");
        (await org.Plain.Client.GetAsync($"/api/v1/career/levels/{senior.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Active");
        (await org.Admin.AuditOfRequestAsync(approved.RequestId!)).Actions().Should().Contain(["CareerLevel.Approved", "CareerLevel.Approved", "CareerTrack.Approved"]);
    }

    [Fact]
    public async Task Ac11_ApprovingATrack_IsRefusedWhenTwoOfItsDraftLevelsOfDifferentAuthorsHaveOneName_AndChangesNothing()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var draft = (await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id)).Expect(HttpStatusCode.Created);
        var byHead = await org.TeamHead.Client.MakeLevelAsync(draft.Id, "Старший");
        var byHr = await org.Hr.Client.MakeLevelAsync(draft.Id, "старший");

        var refused = await org.Hr.Client.PostAsync($"/api/v1/career/tracks/{draft.Id}/approve", ifMatch: draft.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        refused.Json!["blockers"]!.AsArray().Select(blocker => blocker!["reason"]!.GetValue<string>()).Should().Contain("NameTaken");
        (await org.Admin.GetAsync($"/api/v1/career/tracks/{draft.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Draft");
        (await org.Admin.GetAsync($"/api/v1/career/levels/{byHead.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Draft");
        (await org.Admin.GetAsync($"/api/v1/career/levels/{byHr.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Draft");
    }

    [Fact]
    public async Task Ac11_ReactivatingATrackDeactivatedAsADraft_ApprovesItsDraftLevelsAndJournalsEach()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var draft = (await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id)).Expect(HttpStatusCode.Created);
        var junior = await org.TeamHead.Client.MakeLevelAsync(draft.Id);
        var senior = await org.TeamHead.Client.MakeLevelAsync(draft.Id);
        await org.Admin.LinkLevelsAsync(draft.Id, org.Team.Id);
        (await org.Admin.PostAsync($"/api/v1/career/tracks/{draft.Id}/deactivate", ifMatch: (await org.Admin.GetAsync($"/api/v1/career/tracks/{draft.Id}")).ETag)).Expect(HttpStatusCode.OK);

        var activated = await org.Admin.ActivateAsync(draft, org.Team.Id, org.Hr.Client);

        (await org.Plain.Client.GetAsync($"/api/v1/career/levels/{junior.Id}")).Expect(HttpStatusCode.OK).Json!["status"]!.GetValue<string>().Should().Be("Active");
        (await org.Plain.Client.GetAsync($"/api/v1/career/levels/{senior.Id}")).Expect(HttpStatusCode.OK).Json!["status"]!.GetValue<string>().Should().Be("Active");
        (await org.Admin.AuditOfRequestAsync(activated.RequestId!)).Actions().Count(action => action == "CareerLevel.Approved").Should().Be(2);
    }

    [Fact]
    public async Task Ac11_ReactivatingATrackDeactivatedAsADraft_IsRefusedWhenTwoOfItsDraftLevelsOfDifferentAuthorsHaveOneName_AndChangesNothing()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var draft = (await CreateTrackAsync(org.TeamHead.Client, org.Team.Id, direction.Id)).Expect(HttpStatusCode.Created);
        var byHead = await org.TeamHead.Client.MakeLevelAsync(draft.Id, "Старший");
        var byHr = await org.Hr.Client.MakeLevelAsync(draft.Id, "старший");
        var deactivated = (await org.Admin.PostAsync($"/api/v1/career/tracks/{draft.Id}/deactivate", ifMatch: draft.ETag)).Expect(HttpStatusCode.OK);

        var refused = await org.Hr.Client.PostAsync($"/api/v1/career/tracks/{draft.Id}/activate", ifMatch: deactivated.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        refused.Json!["blockers"]!.AsArray().Select(blocker => blocker!["reason"]!.GetValue<string>()).Should().Contain("NameTaken");
        (await org.Admin.GetAsync($"/api/v1/career/tracks/{draft.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Inactive");
        (await org.Admin.GetAsync($"/api/v1/career/levels/{byHead.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Draft");
        (await org.Admin.GetAsync($"/api/v1/career/levels/{byHr.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Draft");
    }

    [Fact]
    public async Task Ac11_LevelOrder_FollowsTheListedLevels_LeavesTheUnlistedOnesInTheirOrderAtTheEnd_AndChangesTheTracksVersion()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var levels = new List<ApiResponse>();
        foreach (var name in new[] { "Один", "Два", "Три", "Четыре" })
        {
            levels.Add(await org.Admin.MakeLevelAsync(track.Id, name));
        }

        track = await org.Admin.ActivateAsync(track, org.Team.Id);
        var reordered = (await org.Admin.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { levels[2].Id, levels[0].Id } }, track.ETag)).Expect(HttpStatusCode.OK);
        (await LevelsOfAsync(org.Admin, track.Id)).Should().Equal("Три", "Один", "Два", "Четыре");
        var partial = (await org.Hr.Client.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { levels[3].Id } }, reordered.ETag)).Expect(HttpStatusCode.OK);

        reordered.ETag.Should().NotBe(track.ETag, "the order is part of the track");
        (await LevelsOfAsync(org.Plain.Client, track.Id)).Should().Equal("Четыре", "Три", "Один", "Два");
        partial.Json!["version"]!.GetValue<int>().Should().BeGreaterThan(reordered.Json!["version"]!.GetValue<int>());
    }

    [Fact]
    public async Task Ac11_LevelOrder_IsRefusedForAnUnknownLevelAFutureNotAKeeperAndAStaleTrack()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var other = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var level = await org.Admin.MakeLevelAsync(track.Id);
        var foreign = await org.Admin.MakeLevelAsync(other.Id);
        track = await org.Admin.ActivateAsync(track, org.Team.Id);

        var unknown = await org.Admin.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { Guid.NewGuid() } }, track.ETag);
        var ofAnotherTrack = await org.Admin.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { foreign.Id } }, track.ETag);
        var byHead = await org.TeamHead.Client.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { level.Id } }, track.ETag);
        var byHrOfAnotherUnit = await org.SiblingHr.Client.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { level.Id } }, track.ETag);
        var stale = await org.Admin.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { level.Id } }, "\"999\"");
        var unknownTrack = await org.Admin.PutAsync($"/api/v1/career/tracks/{Guid.NewGuid()}/level-order", new { levelIds = new[] { level.Id } }, track.ETag);

        unknown.FieldErrors("levelIds").Should().Equal(UnavailableMessage);
        ofAnotherTrack.FieldErrors("levelIds").Should().Equal(UnavailableMessage);
        byHead.Status.Should().Be(HttpStatusCode.Forbidden);
        byHrOfAnotherUnit.Status.Should().Be(HttpStatusCode.Forbidden);
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        unknownTrack.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac11_LevelOrder_WithAStaleTrackThatWouldMoveLevels_IsRefusedWith412_AndMovesAndJournalsNothing()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var levels = new List<ApiResponse>();
        foreach (var name in new[] { "Один", "Два", "Три" })
        {
            levels.Add(await org.Admin.MakeLevelAsync(track.Id, name));
        }

        var stale = await org.Admin.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { levels[2].Id, levels[1].Id, levels[0].Id } }, "\"999\"");

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        (await LevelsOfAsync(org.Admin, track.Id)).Should().Equal(["Один", "Два", "Три"], "the levels are saved one by one before the track's version is checked, and the refusal must undo them");
        (await org.Admin.AuditOfRequestAsync(stale.RequestId!)).Should().BeEmpty("the journal rows of the levels saved before the refusal are undone with them");
    }

    [Fact]
    public async Task Ac17_LevelOrder_JournalsEachLevelThatMovedOnceWithItsOldAndNewPlace_AndRaisesOnlyTheirVersions()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var levels = new List<ApiResponse>();
        foreach (var name in new[] { "Один", "Два", "Три", "Четыре" })
        {
            levels.Add(await org.Admin.MakeLevelAsync(track.Id, name));
        }

        var reordered = (await org.Admin.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { levels[2].Id, levels[0].Id } }, track.ETag)).Expect(HttpStatusCode.OK);

        var journal = await org.Admin.AuditOfRequestAsync(reordered.RequestId!);
        journal.Actions().Should().Equal(["CareerLevel.Updated", "CareerLevel.Updated", "CareerLevel.Updated"], "the level that keeps its place and the track itself are not journaled");
        journal.Select(row => $"{row!["entityId"]!.GetValue<string>()}:{row["oldValue"]!["position"]!.GetValue<int>()}>{row["newValue"]!["position"]!.GetValue<int>()}")
            .Should().BeEquivalentTo([$"{levels[2].Id}:3>1", $"{levels[0].Id}:1>2", $"{levels[1].Id}:2>3"], "each row names the place the level had before the request, not the one it was set aside under");
        var versions = (await org.Admin.GetAsync($"/api/v1/career/levels?trackId={track.Id}&pageSize=200")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray()
            .ToDictionary(item => item!["id"]!.GetValue<Guid>(), item => item!["version"]!.GetValue<int>());
        foreach (var moved in new[] { levels[0], levels[1], levels[2] })
        {
            versions[moved.Id].Should().Be(moved.Json!["version"]!.GetValue<int>() + 1, "a level moves in one save of its own");
        }

        versions[levels[3].Id].Should().Be(levels[3].Json!["version"]!.GetValue<int>());
    }

    [Fact]
    public async Task Ac11_Levels_AreListedInTheOrderOfTheirTrack_AndFilteredByTrack()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var other = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var first = await org.Admin.MakeLevelAsync(track.Id, "Я-первый");
        var second = await org.Admin.MakeLevelAsync(track.Id, "А-второй");
        await org.Admin.MakeLevelAsync(other.Id);
        await org.Admin.ActivateAsync(track, org.Team.Id);

        var listed = (await org.Plain.Client.GetAsync($"/api/v1/career/levels?trackId={track.Id}")).Expect(HttpStatusCode.OK);

        listed.ItemIds().Should().Equal(first.Id.ToString(), second.Id.ToString());
    }

    [Fact]
    public async Task Ac24_Tracks_AreFilteredByDirectionUnitAndStatus()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var other = await org.Admin.MakeDirectionAsync();
        var ofTeam = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var ofSibling = await org.Admin.MakeTrackAsync(org.Sibling.Id, direction.Id);
        var ofOther = await org.Admin.MakeTrackAsync(org.Team.Id, other.Id);
        (await org.Admin.PostAsync($"/api/v1/career/tracks/{ofSibling.Id}/deactivate", ifMatch: ofSibling.ETag)).Expect(HttpStatusCode.OK);

        var byDirection = await TrackIdsAsync(org.Admin, $"directionId={direction.Id}");
        var byUnit = await TrackIdsAsync(org.Admin, $"ownerOrgUnitId={org.Sibling.Id}");
        var byUnitAndDirection = await TrackIdsAsync(org.Admin, $"ownerOrgUnitId={org.Team.Id}&directionId={other.Id}");
        var byStatus = await TrackIdsAsync(org.Admin, $"directionId={direction.Id}&status=Active");

        byDirection.Should().BeEquivalentTo([ofTeam.Id.ToString(), ofSibling.Id.ToString()]);
        byUnit.Should().Equal(ofSibling.Id.ToString());
        byUnitAndDirection.Should().Equal(ofOther.Id.ToString());
        byStatus.Should().Equal(ofTeam.Id.ToString());
    }

    [Fact]
    public async Task Ac9_Tracks_AreListedAndReadEachWithTheNameOfItsOwnUnit()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var direction = await org.Admin.MakeDirectionAsync();
        var ofTeam = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var ofSibling = await org.Admin.MakeTrackAsync(org.Sibling.Id, direction.Id);
        var ofRoot = await org.Admin.MakeTrackAsync(org.Root.Id, direction.Id);
        var unitOf = new Dictionary<Guid, string>
        {
            [ofTeam.Id] = org.Team.Json!["name"]!.GetValue<string>(),
            [ofSibling.Id] = org.Sibling.Json!["name"]!.GetValue<string>(),
            [ofRoot.Id] = org.Root.Json!["name"]!.GetValue<string>(),
        };

        var listed = (await org.Admin.GetAsync($"/api/v1/career/tracks?directionId={direction.Id}&pageSize=200")).Expect(HttpStatusCode.OK);
        var read = new Dictionary<Guid, string>();
        foreach (var id in unitOf.Keys)
        {
            read[id] = (await org.Admin.GetAsync($"/api/v1/career/tracks/{id}")).Expect(HttpStatusCode.OK).Json!["ownerOrgUnitName"]!.GetValue<string>();
        }

        listed.Json!["items"]!.AsArray()
            .ToDictionary(item => item!["id"]!.GetValue<Guid>(), item => item!["ownerOrgUnitName"]!.GetValue<string>())
            .Should().BeEquivalentTo(unitOf, "each row of the list names the unit of its own track");
        read.Should().BeEquivalentTo(unitOf, "a track read on its own names its unit the same");
    }

    private static Task<ApiResponse> CreateTrackAsync(ApiClient client, Guid unitId, Guid directionId) =>
        client.PostAsync("/api/v1/career/tracks", new { name = Scenarios.Unique("Трек"), ownerOrgUnitId = unitId, directionId });

    private static Task<ApiResponse> NewLevelAsync(ApiClient client, Guid trackId) =>
        client.PostAsync("/api/v1/career/levels", new { name = Scenarios.Unique("Уровень"), trackId });

    private static Task<ApiResponse> RenameAsync(ApiClient client, ApiResponse track, string name) =>
        client.PutAsync($"/api/v1/career/tracks/{track.Id}", new { name }, track.ETag);

    private static async Task<string[]> TrackNamesAsync(ApiClient client, string name) =>
        (await client.GetAsync($"/api/v1/career/tracks?q={Uri.EscapeDataString(name)}&pageSize=200")).Expect(HttpStatusCode.OK).ItemNames();

    private static async Task<string[]> TrackIdsAsync(ApiClient client, string query) =>
        (await client.GetAsync($"/api/v1/career/tracks?{query}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();

    private static async Task<string[]> LevelsOfAsync(ApiClient client, Guid trackId) =>
        (await client.GetAsync($"/api/v1/career/levels?trackId={trackId}&pageSize=200")).Expect(HttpStatusCode.OK).ItemNames();
}
