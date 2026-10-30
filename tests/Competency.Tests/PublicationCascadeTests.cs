using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The one rule of making career entries active: an approval publishes every draft the entry refers to or nothing at all, shows the list
/// first and takes the tag of the list the user confirmed, journals each entry on its own, and keeps what a keeper writes from referring to
/// a draft that is not published.
/// </summary>
public sealed class PublicationCascadeTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string DraftReference = "Выбрана черновая запись — сначала утвердите её";
    private const int FieldAbsent = -1;

    [Fact]
    public async Task Ac107_Ac110_Ac113_Ac114_ApprovingACompetency_PublishesTheDraftsItReferstoOnTheTagOfTheListShown_EachJournaledOnItsOwn()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (direction, scale, competency) = await DraftCompetencyAsync(org);
        var path = $"/api/v1/career/competencies/{competency.Id}";

        var preview = (await org.Hr.Client.GetAsync($"{path}/publication")).Expect(HttpStatusCode.OK).Json!;
        var withoutTag = await org.Hr.Client.PostAsync($"{path}/approve", ifMatch: competency.ETag);
        var wrongTag = await org.Hr.Client.PostAsync($"{path}/approve?cascade=0000", ifMatch: competency.ETag);
        var renamed = (await org.TeamHead.Client.PutAsync($"/api/v1/career/directions/{direction.Id}", new { name = Scenarios.Unique("Правка") }, direction.ETag)).Expect(HttpStatusCode.OK);
        var changedPreview = (await org.Hr.Client.GetAsync($"{path}/publication")).Expect(HttpStatusCode.OK).Json!;
        var staleTag = await org.Hr.Client.PostAsync($"{path}/approve?cascade={Uri.EscapeDataString(preview["cascadeTag"]!.GetValue<string>())}", ifMatch: competency.ETag);
        var approved = (await org.Hr.Client.PostAsync(
            $"{path}/approve?cascade={Uri.EscapeDataString(changedPreview["cascadeTag"]!.GetValue<string>())}",
            ifMatch: competency.ETag)).Expect(HttpStatusCode.OK);

        Entries(preview).Should().BeEquivalentTo([("Direction", direction.Id), ("Scale", scale.Id)]);
        preview["blockers"]!.AsArray().Should().BeEmpty();
        preview["moreCount"]!.GetValue<int>().Should().Be(0);
        HiddenEntryCount(preview).Should().Be(0, "a keeper reads every draft the approval publishes");
        foreach (var refused in new[] { withoutTag, wrongTag, staleTag })
        {
            refused.Status.Should().Be(HttpStatusCode.PreconditionFailed);
            refused.Json!["publication"]!.GetValue<string>().Should().Be($"{path}/publication");
        }

        changedPreview["cascadeTag"]!.GetValue<string>().Should().NotBe(preview["cascadeTag"]!.GetValue<string>(), "the list changed with the direction's version");
        renamed.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        approved.Json!["status"]!.GetValue<string>().Should().Be("Active");
        (await StatusAsync(org.Admin, "directions", direction.Id)).Should().Be("Active");
        (await StatusAsync(org.Admin, "scales", scale.Id)).Should().Be("Active");
        var journal = await org.Admin.AuditOfRequestAsync(approved.RequestId!);
        journal.Where(row => row!["action"]!.GetValue<string>().EndsWith(".Approved", StringComparison.Ordinal))
            .Select(row => row!["action"]!.GetValue<string>()).Should().BeEquivalentTo(["CareerCompetency.Approved", "CareerDirection.Approved", "CompetencyScale.Approved"]);
        journal.Single(row => row!["action"]!.GetValue<string>() == "CareerCompetency.Approved")!["reason"].Should().BeNull();
        journal.Single(row => row!["action"]!.GetValue<string>() == "CareerDirection.Approved")!["reason"]!.GetValue<string>().Should().Contain(competency.Id.ToString());
    }

    [Fact]
    public async Task Ac109_Ac113_APublicationThatSomethingStopsLeavesEveryDraftAsItWas_AndThenGoesThroughOnceNothingIsInTheWay()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var head = org.TeamHead.Client;
        var direction = (await head.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Область") })).Expect(HttpStatusCode.Created);
        var track = (await head.PostAsync("/api/v1/career/tracks", new { name = Scenarios.Unique("Трек"), ownerOrgUnitId = org.Team.Id, directionId = direction.Id })).Expect(HttpStatusCode.Created);
        var level = await head.MakeLevelAsync(track.Id);
        var path = $"/api/v1/career/tracks/{track.Id}";

        var refused = await org.Hr.Client.PostAsync($"{path}/approve", ifMatch: track.ETag);
        var linked = await org.Admin.LinkToNewPositionAsync(level, org.Team.Id);
        var preview = (await org.Hr.Client.GetAsync($"{path}/publication")).Expect(HttpStatusCode.OK).Json!;
        var approved = (await org.Hr.Client.PostAsync(
            $"{path}/approve?cascade={Uri.EscapeDataString(preview["cascadeTag"]!.GetValue<string>())}",
            ifMatch: (await org.Admin.GetAsync(path)).ETag)).Expect(HttpStatusCode.OK);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["blockers"]!.AsArray().Select(blocker => $"{blocker!["kind"]}/{blocker["reason"]}").Should().Equal("Level/NoActivePosition");
        (await org.Admin.AuditOfRequestAsync(refused.RequestId!)).Should().BeEmpty();
        Entries(preview).Should().BeEquivalentTo([("Direction", direction.Id), ("Level", level.Id)]);
        approved.Json!["status"]!.GetValue<string>().Should().Be("Active");
        (await StatusAsync(org.Admin, "directions", direction.Id)).Should().Be("Active");
        (await StatusAsync(org.Admin, "levels", level.Id)).Should().Be("Active");
        (await org.Admin.AuditOfRequestAsync(approved.RequestId!)).Actions().Where(action => action.EndsWith(".Approved", StringComparison.Ordinal))
            .Should().BeEquivalentTo(["CareerTrack.Approved", "CareerDirection.Approved", "CareerLevel.Approved"]);
        linked.Json!["id"]!.GetValue<Guid>().Should().Be(level.Id);
    }

    [Fact]
    public async Task Ac109_AnApprovalAndItsPreview_AreAKeepersAlone_AndADraftIsInvisibleToAUserOutsideItsUnit()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (_, _, competency) = await DraftCompetencyAsync(org);
        var path = $"/api/v1/career/competencies/{competency.Id}";

        var byHead = await org.TeamHead.Client.PostAsync($"{path}/approve", ifMatch: competency.ETag);
        var headPreview = await org.TeamHead.Client.GetAsync($"{path}/publication");
        var plainPreview = await org.Plain.Client.GetAsync($"{path}/publication");
        var plainApproval = await org.Plain.Client.PostAsync($"{path}/approve", ifMatch: competency.ETag);

        byHead.Status.Should().Be(HttpStatusCode.Forbidden);
        headPreview.Status.Should().Be(HttpStatusCode.Forbidden, "only whoever may approve is shown what the approval would publish");
        plainPreview.Status.Should().Be(HttpStatusCode.NotFound);
        plainApproval.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac106_Ac109_Ac110_PublishingAVersion_ListsItsDraftTrackAndLevelsAndCompetencies_AndOnlyTheResponsibleHrsStepPublishesThem()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org, competencyAuthor: org.TeamHead.Client)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var path = $"/api/v1/career/matrix-versions/{kit.VersionId}";

        var superior = (await org.DepartmentHead.Client.GetAsync($"{path}/publication")).Expect(HttpStatusCode.OK).Json!;
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        var hr = (await org.Hr.Client.GetAsync($"{path}/publication")).Expect(HttpStatusCode.OK).Json!;
        var withoutTag = await org.Hr.Client.PostAsync($"{path}/approve", ifMatch: await kit.ETagAsync());
        var plain = await org.Plain.Client.GetAsync($"{path}/publication");
        var published = (await org.Hr.Client.PostAsync(
            $"{path}/approve?cascade={Uri.EscapeDataString(hr["cascadeTag"]!.GetValue<string>())}",
            ifMatch: await kit.ETagAsync())).Expect(HttpStatusCode.OK);

        var expected = new[] { ("Track", kit.Track.Id), ("Level", kit.Junior.Id), ("Level", kit.Senior.Id), ("Competency", kit.Critical.Id), ("Competency", kit.Profile.Id) };
        Entries(superior).Should().BeEquivalentTo(expected);
        Entries(hr).Should().BeEquivalentTo(expected);
        superior["publishingActions"]!.AsArray().Should().BeEmpty("the superior's approval publishes nothing");
        hr["publishingActions"]!.AsArray().Select(action => action!.GetValue<string>()).Should().Equal("Approve");
        withoutTag.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        withoutTag.Json!["publication"]!.GetValue<string>().Should().Be($"{path}/publication");
        plain.Status.Should().Be(HttpStatusCode.NotFound);
        published.Json!["status"]!.GetValue<string>().Should().Be("Published");
        (await StatusAsync(org.Admin, "tracks", kit.Track.Id)).Should().Be("Active");
        (await StatusAsync(org.Admin, "competencies", kit.Critical.Id)).Should().Be("Active");
        (await org.Admin.AuditOfRequestAsync(published.RequestId!)).Actions().Where(action => action.EndsWith(".Approved", StringComparison.Ordinal))
            .Should().BeEquivalentTo(["MatrixVersion.Approved", "CareerTrack.Approved", "CareerLevel.Approved", "CareerLevel.Approved", "CareerCompetency.Approved", "CareerCompetency.Approved"]);
    }

    [Fact]
    public async Task Ac115_AKeepersNewEntryThatRefersToADraft_IsCreatedAsADraft_AndOneThatRefersToNothingDraftIsActive()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var draftDirection = (await org.TeamHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Область") })).Expect(HttpStatusCode.Created);
        var keeper = await org.Admin.GrantHrRoleAsync(fixture.Host, org.TeamHead);

        var referring = (await keeper.Client.PostAsync(
            "/api/v1/career/competencies",
            new { name = Scenarios.Unique("Ссылка"), description = (string?)null, directionId = (Guid?)draftDirection.Id, scaleId = (Guid?)null })).Expect(HttpStatusCode.Created);
        var plain = await keeper.Client.MakeCompetencyAsync();

        draftDirection.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        referring.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        plain.Json!["status"]!.GetValue<string>().Should().Be("Active");
    }

    [Fact]
    public async Task Ac116_ChangingAnActiveEntryToReferToADraft_IsRefusedAsAFieldError_AndTheEntryStaysAsItWas()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var draftDirection = (await org.TeamHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Область") })).Expect(HttpStatusCode.Created);
        var competency = await org.Admin.MakeCompetencyAsync();

        var refused = await org.Admin.PutAsync(
            $"/api/v1/career/competencies/{competency.Id}",
            new { name = competency.Json!["name"]!.GetValue<string>(), description = (string?)null, directionId = (Guid?)draftDirection.Id, scaleId = (Guid?)null },
            competency.ETag);

        refused.Status.Should().Be(HttpStatusCode.BadRequest);
        refused.FieldErrors("directionId").Should().Equal(DraftReference);
        var after = (await org.Admin.GetAsync($"/api/v1/career/competencies/{competency.Id}")).Expect(HttpStatusCode.OK);
        after.ETag.Should().Be(competency.ETag);
        after.Json!["directionId"].Should().BeNull();
        after.Json["status"]!.GetValue<string>().Should().Be("Active");
    }

    [Fact]
    public async Task Ac107_Ac114_ApprovingABlock_PublishesTheDraftCompetenciesOfItsComposition_EachWithANewVersionInItsHistory()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var competency = await org.TeamHead.Client.MakeCompetencyAsync();
        var block = (await org.TeamHead.Client.PostAsync(
            "/api/v1/career/blocks",
            new { name = Scenarios.Unique("Блок"), scopeUnitId = (Guid?)org.Team.Id, competencyIds = new[] { competency.Id } })).Expect(HttpStatusCode.Created);
        var path = $"/api/v1/career/blocks/{block.Id}";
        var before = await VersionsAsync(org.Admin, competency.Id);

        var preview = (await org.Hr.Client.GetAsync($"{path}/publication")).Expect(HttpStatusCode.OK).Json!;
        (await org.Hr.Client.PostAsync(
            $"{path}/approve?cascade={Uri.EscapeDataString(preview["cascadeTag"]!.GetValue<string>())}",
            ifMatch: block.ETag)).Expect(HttpStatusCode.OK);

        var after = await VersionsAsync(org.Admin, competency.Id);
        Entries(preview).Should().BeEquivalentTo([("Competency", competency.Id)]);
        (await StatusAsync(org.Admin, "competencies", competency.Id)).Should().Be("Active");
        before.Select(version => version.Status).Should().Equal("Draft");
        after.Select(version => version.Status).Should().Equal("Active", "Draft");
        after[0].Number.Should().Be(before.Single().Number + 1);
    }

    [Fact]
    public async Task Ac144_ADraftBlockOutsideTheResponsibleHrsUnits_IsPublishedWhenThatHrIsAlsoAnAdministrator_WhoeverAcceptsTheirChanges()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var draftCompetency = await org.TeamHead.Client.MakeCompetencyAsync();
        var head = await org.Admin.GrantHrRoleAsync(fixture.Host, org.TeamHead);
        var block = (await head.Client.PostAsync(
            "/api/v1/career/blocks",
            new { name = Scenarios.Unique("Блок"), scopeUnitId = (Guid?)org.Department.Id, competencyIds = new[] { draftCompetency.Id } })).Expect(HttpStatusCode.Created);
        var responsible = await org.Admin.HrOfAsync(fixture.Host, org.Team.Id);
        var kit = await MatrixKit.CreateAsync(org, author: head.Client);
        (await kit.SaveAsync(head.Client, ContentWithBlock(kit, block.Id, draftCompetency.Id, seniorScore: 3))).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(head.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        (await kit.SaveAsync(responsible.Client, ContentWithBlock(kit, block.Id, draftCompetency.Id, seniorScore: 4))).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(responsible.Client, "return", new { comment = "Поправил" })).Expect(HttpStatusCode.OK);

        var asOnlyHr = await kit.TransitAsync(head.Client, "accept-changes");
        await AddAdministratorRoleAsync(org.Admin, responsible);
        var asAlsoAdministrator = await kit.TransitAsync(head.Client, "accept-changes");

        asOnlyHr.Status.Should().Be(HttpStatusCode.Conflict, asOnlyHr.Body);
        asOnlyHr.Json!["blockers"]!.AsArray().Select(blocker => $"{blocker!["id"]}/{blocker["reason"]}").Should().Equal($"{block.Id}/NoRight");
        asAlsoAdministrator.Status.Should().Be(HttpStatusCode.OK, asAlsoAdministrator.Body);
        asAlsoAdministrator.Json!["status"]!.GetValue<string>().Should().Be("Published");
        (await StatusAsync(org.Admin, "blocks", block.Id)).Should().Be("Active");
        (await StatusAsync(org.Admin, "competencies", draftCompetency.Id)).Should().Be("Active");
    }

    [Fact]
    public async Task Ac109_Ac110_APreview_NamesEveryBlockerTheReaderSees_WhateverItsKind()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var head = org.TeamHead.Client;
        var directionName = Scenarios.Unique("Область");
        var direction = (await head.PostAsync("/api/v1/career/directions", new { name = directionName })).Expect(HttpStatusCode.Created);
        var track = (await head.PostAsync("/api/v1/career/tracks", new { name = Scenarios.Unique("Трек"), ownerOrgUnitId = org.Team.Id, directionId = direction.Id })).Expect(HttpStatusCode.Created);
        var level = await head.MakeLevelAsync(track.Id);
        await org.Admin.MakeDirectionAsync(directionName.ToUpperInvariant());

        var preview = (await org.Hr.Client.GetAsync($"/api/v1/career/tracks/{track.Id}/publication")).Expect(HttpStatusCode.OK).Json!;

        preview["blockers"]!.AsArray().Select(blocker => $"{blocker!["kind"]}/{blocker["reason"]}/{blocker["id"]}").Should()
            .BeEquivalentTo([$"Direction/NameTaken/{direction.Id}", $"Level/NoActivePosition/{level.Id}"]);
    }

    [Fact]
    public async Task Ac109_Ac110_ABlockerOfALevelOutsideTheVersion_IsNotNamedToTheSuperiorApproverWhoCannotReadIt_InThePreviewNorInTheRefusal()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var responsible = await org.Admin.GrantHrRoleAsync(fixture.Host, org.TeamHead);
        (await org.Admin.AssignHrAsync(org.Team.Id, responsible.EmployeeId)).Expect(HttpStatusCode.NoContent);
        var kit = await (await MatrixKit.CreateAsync(org, author: responsible.Client)).PrepareAsync();
        var outside = await responsible.Client.MakeLevelAsync(kit.Track.Id, kit.Junior.Json!["name"]!.GetValue<string>().ToUpperInvariant());
        await org.Admin.LinkToNewPositionAsync(outside, org.Team.Id);
        (await kit.TransitAsync(responsible.Client, "submit")).Expect(HttpStatusCode.OK);
        var path = $"/api/v1/career/matrix-versions/{kit.VersionId}/publication";
        var inContent = $"Level/NameTaken/{kit.Junior.Id}";
        var notInContent = $"Level/NameTaken/{outside.Id}";

        var bySuperior = (await org.DepartmentHead.Client.GetAsync(path)).Expect(HttpStatusCode.OK).Json!;
        var byResponsible = (await responsible.Client.GetAsync(path)).Expect(HttpStatusCode.OK).Json!;
        var byAdmin = (await org.Admin.GetAsync(path)).Expect(HttpStatusCode.OK).Json!;
        var refused = await kit.TransitAsync(org.DepartmentHead.Client, "approve");

        foreach (var full in new[] { byResponsible, byAdmin })
        {
            Blockers(full).Should().BeEquivalentTo([inContent, notInContent]);
            full["moreCount"]!.GetValue<int>().Should().Be(0);
        }

        Blockers(bySuperior).Should().Equal(inContent);
        bySuperior["moreCount"]!.GetValue<int>().Should().Be(1, "the level outside the version is counted but not named");
        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        Blockers(refused.Json!).Should().Equal(inContent);
        refused.Json!["moreCount"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task Ac110_Ac112_APreviewOfAVersion_ListsOnlyTheDraftsItsReaderMayRead_CountsTheRest_AndCarriesOneTagForTheWholeList()
    {
        var hidden = await HiddenLevelAsync();
        var kit = hidden.Kit;

        var bySuperior = (await hidden.Org.DepartmentHead.Client.GetAsync(hidden.PreviewPath)).Expect(HttpStatusCode.OK).Json!;
        var byResponsible = (await hidden.Responsible.Client.GetAsync(hidden.PreviewPath)).Expect(HttpStatusCode.OK).Json!;
        var byAdmin = (await hidden.Org.Admin.GetAsync(hidden.PreviewPath)).Expect(HttpStatusCode.OK).Json!;

        using var scope = new AssertionScope();
        (string Kind, Guid Id)[] whole = [("Track", kit.Track.Id), ("Level", kit.Junior.Id), ("Level", kit.Senior.Id), ("Level", hidden.Outside.Id)];
        foreach (var full in new[] { byResponsible, byAdmin })
        {
            Entries(full).Should().BeEquivalentTo(whole);
            HiddenEntryCount(full).Should().Be(0, "whoever may read every draft sees them all");
            full["cascadeTag"]!.GetValue<string>().Should().Be(bySuperior["cascadeTag"]!.GetValue<string>(), "one tag stands for the whole list, whoever reads it");
        }

        Entries(bySuperior).Should().BeEquivalentTo(whole.Where(entry => entry.Id != hidden.Outside.Id));
        HiddenEntryCount(bySuperior).Should().Be(1, "the level outside the version is counted but not listed");
        Strings(bySuperior).Where(text => text.Contains(hidden.Outside.Id.ToString()) || text.Contains(hidden.Name)).Should()
            .BeEmpty("nothing of a draft the reader may not read is shown");
        Blockers(bySuperior).Should().BeEmpty();
        bySuperior["moreCount"]!.GetValue<int>().Should().Be(0);
        bySuperior["publishingActions"]!.AsArray().Select(action => action!.GetValue<string>()).Should().Equal("Approve");
    }

    [Fact]
    public async Task Ac112_ApprovingUnderThePreviewOfAReaderWhoCannotReadADraft_PublishesTheWholeList_AndIsRefusedOnceThatDraftChanged()
    {
        var hidden = await HiddenLevelAsync();
        var kit = hidden.Kit;
        var superior = hidden.Org.DepartmentHead.Client;
        var approve = $"/api/v1/career/matrix-versions/{kit.VersionId}/approve";

        var shown = (await superior.GetAsync(hidden.PreviewPath)).Expect(HttpStatusCode.OK).Json!;
        (await hidden.Responsible.Client.PutAsync(
            $"/api/v1/career/levels/{hidden.Outside.Id}",
            new { name = Scenarios.Unique("Переименован") },
            hidden.Outside.ETag)).Expect(HttpStatusCode.OK);
        var stale = await superior.PostAsync(
            $"{approve}?cascade={Uri.EscapeDataString(shown["cascadeTag"]!.GetValue<string>())}",
            ifMatch: await kit.ETagAsync());
        var afterStale = (await kit.StatusAsync(), await StatusAsync(hidden.Org.Admin, "levels", hidden.Outside.Id));
        var fresh = (await superior.GetAsync(hidden.PreviewPath)).Expect(HttpStatusCode.OK).Json!;
        var published = await superior.PostAsync(
            $"{approve}?cascade={Uri.EscapeDataString(fresh["cascadeTag"]!.GetValue<string>())}",
            ifMatch: await kit.ETagAsync());

        using var scope = new AssertionScope();
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, "a change of the draft the reader cannot read changes the list they confirmed");
        stale.Json!["publication"]!.GetValue<string>().Should().Be(hidden.PreviewPath);
        afterStale.Should().Be(("OnReview", "Draft"), "a refused publication changes nothing");
        HiddenEntryCount(fresh).Should().Be(1);
        fresh["cascadeTag"]!.GetValue<string>().Should().NotBe(shown["cascadeTag"]!.GetValue<string>(), "the version of the unlisted draft is part of the tag");
        published.Status.Should().Be(HttpStatusCode.OK, published.Body);
        published.Json!["status"]!.GetValue<string>().Should().Be("Published");
        (await StatusAsync(hidden.Org.Admin, "levels", hidden.Outside.Id)).Should().Be("Active", "the unlisted draft is published with the rest");
        (await hidden.Org.Admin.AuditOfRequestAsync(published.RequestId!)).Actions().Where(action => action.EndsWith(".Approved", StringComparison.Ordinal))
            .Should().BeEquivalentTo(["MatrixVersion.Approved", "CareerTrack.Approved", "CareerLevel.Approved", "CareerLevel.Approved", "CareerLevel.Approved"]);
    }

    private static async Task AddAdministratorRoleAsync(ApiClient admin, Member member)
    {
        var current = await admin.GetUserAsync(member.Id);
        (await admin.PutAsync(
            $"/api/v1/users/{member.Id}",
            new { email = member.User.Email, roles = new[] { Scenarios.Hr, Scenarios.GlobalAdmin }, employeeId = member.EmployeeId },
            current.ETag)).Expect(HttpStatusCode.OK);
    }

    private static JsonObject ContentWithBlock(MatrixKit kit, Guid blockId, Guid competencyId, int seniorScore)
    {
        var content = kit.Content();
        content["competencies"] = new JsonArray();
        content["sharedBlocks"] = new JsonArray(new JsonObject
        {
            ["blockId"] = blockId,
            ["competencyTypes"] = new JsonArray(new JsonObject { ["competencyId"] = competencyId, ["type"] = "Additional" }),
        });
        content["blockWeights"] = new JsonArray(
            new JsonObject { ["levelId"] = kit.Junior.Id, ["blockId"] = blockId, ["percent"] = 100 },
            new JsonObject { ["levelId"] = kit.Senior.Id, ["blockId"] = blockId, ["percent"] = 100 });
        content["requirements"] = new JsonArray(
            new JsonObject { ["levelId"] = kit.Junior.Id, ["competencyId"] = competencyId, ["requiredScore"] = 2 },
            new JsonObject { ["levelId"] = kit.Senior.Id, ["competencyId"] = competencyId, ["requiredScore"] = seniorScore });
        return content;
    }

    private static async Task<(int Number, string Status)[]> VersionsAsync(ApiClient client, Guid competencyId) =>
        [.. (await client.GetAsync($"/api/v1/career/competencies/{competencyId}/versions")).Expect(HttpStatusCode.OK).Json!.AsArray()
            .Select(version => (version!["number"]!.GetValue<int>(), version["status"]!.GetValue<string>()))];

    /// <summary>
    /// A version of the team's matrix in the superior approver's step, whose track holds a draft level outside the version's document, so the
    /// version publishes it though its superior approver cannot read it. The responsible HR is the author, so the superior's approval publishes.
    /// </summary>
    private async Task<HiddenLevel> HiddenLevelAsync()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var responsible = await org.Admin.GrantHrRoleAsync(fixture.Host, org.TeamHead);
        (await org.Admin.AssignHrAsync(org.Team.Id, responsible.EmployeeId)).Expect(HttpStatusCode.NoContent);
        var kit = await (await MatrixKit.CreateAsync(org, author: responsible.Client)).PrepareAsync();
        var name = Scenarios.Unique("Скрытый");
        var outside = await org.Admin.LinkToNewPositionAsync(await responsible.Client.MakeLevelAsync(kit.Track.Id, name), org.Team.Id);
        (await kit.TransitAsync(responsible.Client, "submit")).Expect(HttpStatusCode.OK);
        return new HiddenLevel(org, kit, responsible, outside, name);
    }

    private static int HiddenEntryCount(JsonNode preview) => preview["hiddenEntryCount"]?.GetValue<int>() ?? FieldAbsent;

    private static IEnumerable<string> Strings(JsonNode? node) => node switch
    {
        JsonValue value when value.TryGetValue<string>(out var text) => [text],
        JsonObject properties => properties.SelectMany(property => Strings(property.Value)),
        JsonArray items => items.SelectMany(Strings),
        _ => [],
    };

    private static string[] Blockers(JsonNode body) =>
        [.. body["blockers"]!.AsArray().Select(blocker => $"{blocker!["kind"]}/{blocker["reason"]}/{blocker["id"]}")];

    private static (string Kind, Guid Id)[] Entries(JsonNode preview) =>
        [.. preview["entries"]!.AsArray().Select(entry => (entry!["kind"]!.GetValue<string>(), entry["id"]!.GetValue<Guid>()))];

    private static async Task<string> StatusAsync(ApiClient client, string segment, Guid id) =>
        (await client.GetAsync($"/api/v1/career/{segment}/{id}")).Expect(HttpStatusCode.OK).Json!["status"]!.GetValue<string>();

    private static async Task<(ApiResponse Direction, ApiResponse Scale, ApiResponse Competency)> DraftCompetencyAsync(CareerOrg org)
    {
        var head = org.TeamHead.Client;
        var direction = (await head.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Область") })).Expect(HttpStatusCode.Created);
        var scale = (await head.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Шкала"), points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);
        var competency = (await head.PostAsync(
            "/api/v1/career/competencies",
            new { name = Scenarios.Unique("Компетенция"), description = (string?)null, directionId = (Guid?)direction.Id, scaleId = (Guid?)scale.Id })).Expect(HttpStatusCode.Created);
        return (direction, scale, competency);
    }

    private sealed record HiddenLevel(CareerOrg Org, MatrixKit Kit, Member Responsible, ApiResponse Outside, string Name)
    {
        public string PreviewPath => $"/api/v1/career/matrix-versions/{Kit.VersionId}/publication";
    }
}
