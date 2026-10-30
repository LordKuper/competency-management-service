using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Shared blocks of competencies: who may write one for which units, what a composition may hold, where a block is available, when it
/// can be deleted, and what a matrix version that includes one sees, keeps and is told when the composition changes.
/// </summary>
public sealed class CompetencyBlockTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string EmptyComposition = "Состав блока пуст";
    private const string DraftReference = "Выбрана черновая запись — сначала утвердите её";

    [Fact]
    public async Task Ac128_AKeeperWritesBlocksForAllOrForAUnit_AHeadProposesDraftsForTheirSubtreesOnly_AndOthersWriteNone()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var competency = await org.Admin.MakeCompetencyAsync();

        var byAdmin = await CreateAsync(org.Admin, [competency.Id]);
        var byHr = await CreateAsync(org.Hr.Client, [competency.Id], org.Team.Id);
        var headForAll = await CreateAsync(org.TeamHead.Client, [competency.Id]);
        var headForSibling = await CreateAsync(org.TeamHead.Client, [competency.Id], org.Sibling.Id);
        var headForOwn = await CreateAsync(org.TeamHead.Client, [competency.Id], org.Team.Id);
        var headEmpty = await CreateAsync(org.TeamHead.Client, [], org.Team.Id);
        var byPlain = await CreateAsync(org.Plain.Client, [competency.Id], org.Team.Id);

        byAdmin.Expect(HttpStatusCode.Created).Json!["status"]!.GetValue<string>().Should().Be("Active");
        byHr.Expect(HttpStatusCode.Created).Json!["scopeUnitId"]!.GetValue<Guid>().Should().Be(org.Team.Id);
        headForAll.Status.Should().Be(HttpStatusCode.Forbidden, "no head may write a block for all");
        headForSibling.Status.Should().Be(HttpStatusCode.Forbidden);
        headForOwn.Expect(HttpStatusCode.Created).Json!["status"]!.GetValue<string>().Should().Be("Draft");
        headEmpty.Expect(HttpStatusCode.Created).Json!["status"]!.GetValue<string>().Should().Be("Draft", "a draft may be empty");
        byPlain.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ac127_ABlockHoldsCompetenciesOfAnyScaleOnceEach_ItsNameIsUniqueRegardlessOfCase_AndAnActiveOneNeedsOne()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var otherScale = await org.Admin.MakeScaleAsync();
        var onBase = await org.Admin.MakeCompetencyAsync();
        var onOther = await org.Admin.MakeCompetencyAsync(scaleId: otherScale.Id);
        var draftOfHead = await org.TeamHead.Client.MakeCompetencyAsync();
        var name = Scenarios.Unique("Блок");

        var mixed = (await CreateAsync(org.Admin, [onBase.Id, onOther.Id], name: name)).Expect(HttpStatusCode.Created);
        var twice = await CreateAsync(org.Admin, [onBase.Id, onBase.Id]);
        var namesake = await CreateAsync(org.Admin, [onBase.Id], name: name.ToUpperInvariant());
        var empty = await CreateAsync(org.Admin, []);
        var withDraft = await CreateAsync(org.Admin, [draftOfHead.Id]);
        var emptied = await org.Admin.PutAsync(
            $"/api/v1/career/blocks/{mixed.Id}",
            new { name, scopeUnitId = (Guid?)null, competencyIds = Array.Empty<Guid>() },
            mixed.ETag);

        mixed.Json!["competencies"]!.AsArray().Select(competency => competency!["id"]!.GetValue<Guid>()).Should().Equal(onBase.Id, onOther.Id);
        twice.Status.Should().Be(HttpStatusCode.BadRequest);
        twice.FieldErrors("competencyIds").Should().NotBeEmpty();
        namesake.Status.Should().Be(HttpStatusCode.Conflict);
        empty.Status.Should().Be(HttpStatusCode.BadRequest);
        empty.FieldErrors("competencyIds").Should().Equal(EmptyComposition);
        withDraft.Status.Should().Be(HttpStatusCode.BadRequest);
        withDraft.FieldErrors("competencyIds").Should().NotBeEmpty("a keeper cannot put another author's draft into an active block");
        emptied.Status.Should().Be(HttpStatusCode.BadRequest);
        emptied.FieldErrors("competencyIds").Should().Equal(EmptyComposition);
    }

    [Fact]
    public async Task Ac129_TheBlocksAvailableToAUnit_AreThoseForAllAndThoseOfTheUnitOrAUnitAboveIt()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var competency = await org.Admin.MakeCompetencyAsync();
        var forAll = (await CreateAsync(org.Admin, [competency.Id])).Expect(HttpStatusCode.Created);
        var forDepartment = (await CreateAsync(org.Admin, [competency.Id], org.Department.Id)).Expect(HttpStatusCode.Created);
        var forTeam = (await CreateAsync(org.Admin, [competency.Id], org.Team.Id)).Expect(HttpStatusCode.Created);
        var forSibling = (await CreateAsync(org.Admin, [competency.Id], org.Sibling.Id)).Expect(HttpStatusCode.Created);

        var ofTeam = (await org.Admin.GetAsync($"/api/v1/career/blocks?availableFor={org.Team.Id}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();
        var ofRoot = (await org.Admin.GetAsync($"/api/v1/career/blocks?availableFor={org.Root.Id}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();
        var forAllOnly = (await org.Admin.GetAsync("/api/v1/career/blocks?forAll=true&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();

        ofTeam.Should().Contain([forAll.Id.ToString(), forDepartment.Id.ToString(), forTeam.Id.ToString()]);
        ofTeam.Should().NotContain(forSibling.Id.ToString());
        ofRoot.Should().Contain(forAll.Id.ToString()).And.NotContain([forDepartment.Id.ToString(), forTeam.Id.ToString(), forSibling.Id.ToString()]);
        forAllOnly.Should().Contain(forAll.Id.ToString()).And.NotContain([forDepartment.Id.ToString(), forTeam.Id.ToString()]);
    }

    [Fact]
    public async Task Ac133_ABlockNoMatrixIncludes_IsDeleted_AndOneAMatrixIncludesIsRefusedNamingTheMatrix()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org);
        var free = (await CreateAsync(org.Admin, [kit.Critical.Id])).Expect(HttpStatusCode.Created);
        var used = (await CreateAsync(org.Admin, [kit.Critical.Id, kit.Profile.Id])).Expect(HttpStatusCode.Created);
        (await kit.SaveAsync(org.TeamHead.Client, WithBlock(kit, used.Id, kit.Critical.Id, kit.Profile.Id))).Expect(HttpStatusCode.OK);

        var refused = await org.Admin.DeleteAsync($"/api/v1/career/blocks/{used.Id}", used.ETag);
        var deleted = await org.Admin.DeleteAsync($"/api/v1/career/blocks/{free.Id}", free.ETag);
        var matrices = (await org.Admin.GetAsync($"/api/v1/career/blocks/{used.Id}/matrices")).Expect(HttpStatusCode.OK).Json!.AsArray();

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Be("Общий блок используют матрицы — удалить нельзя");
        deleted.Status.Should().Be(HttpStatusCode.NoContent);
        (await org.Admin.GetAsync($"/api/v1/career/blocks/{free.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
        matrices.Select(row => row!["matrixId"]!.GetValue<Guid>()).Should().Equal(kit.MatrixId);
    }

    [Fact]
    public async Task Ac129_Ac130_ABlockInAVersion_GivesItsCompetenciesToTheCheck_AnUnavailableBlockIsRefusedOnSave_AndOneThatBecameUnavailableOrInactiveIsAProblem()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org);
        var ofSibling = (await CreateAsync(org.Admin, [kit.Critical.Id, kit.Profile.Id], org.Sibling.Id)).Expect(HttpStatusCode.Created);
        var available = (await CreateAsync(org.Admin, [kit.Critical.Id, kit.Profile.Id], org.Department.Id)).Expect(HttpStatusCode.Created);
        var name = available.Json!["name"]!.GetValue<string>();

        var refused = await kit.SaveAsync(org.TeamHead.Client, WithBlock(kit, ofSibling.Id, kit.Critical.Id, kit.Profile.Id));
        (await kit.SaveAsync(org.TeamHead.Client, WithBlock(kit, available.Id, kit.Critical.Id, kit.Profile.Id))).Expect(HttpStatusCode.OK);
        var complete = await ProblemsAsync(kit);
        var moved = (await org.Admin.PutAsync(
            $"/api/v1/career/blocks/{available.Id}",
            new { name, scopeUnitId = (Guid?)org.Sibling.Id, competencyIds = new[] { kit.Critical.Id, kit.Profile.Id } },
            available.ETag)).Expect(HttpStatusCode.OK);
        var unavailable = await ProblemsAsync(kit);
        var restored = (await org.Admin.PutAsync(
            $"/api/v1/career/blocks/{available.Id}",
            new { name, scopeUnitId = (Guid?)org.Department.Id, competencyIds = new[] { kit.Critical.Id, kit.Profile.Id } },
            moved.ETag)).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/career/blocks/{available.Id}/deactivate", ifMatch: restored.ETag)).Expect(HttpStatusCode.OK);
        var inactive = await ProblemsAsync(kit);

        refused.Status.Should().Be(HttpStatusCode.BadRequest);
        refused.FieldErrors("content").Should().Equal($"Общий блок «{ofSibling.Json!["name"]!.GetValue<string>()}» недоступен подразделению матрицы");
        complete.Should().BeEmpty();
        unavailable.Should().Equal("SharedBlockUnavailable");
        inactive.Should().Equal("InactiveEntry");
    }

    [Fact]
    public async Task Ac133_Ac191_AChangedBlockCompositionLeavesThePublishedVersionAsItWas_AndAsksTheHeadForANewVersion()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org);
        var block = (await CreateAsync(org.Admin, [kit.Critical.Id, kit.Profile.Id])).Expect(HttpStatusCode.Created);
        (await kit.SaveAsync(org.TeamHead.Client, WithBlock(kit, block.Id, kit.Critical.Id, kit.Profile.Id))).Expect(HttpStatusCode.OK);
        await kit.PublishAsync();
        var before = (await org.TeamHead.Client.ItemsAsync()).ItemOf(kit.MatrixId);

        var changed = (await org.Admin.PutAsync(
            $"/api/v1/career/blocks/{block.Id}",
            new { name = block.Json!["name"]!.GetValue<string>(), scopeUnitId = (Guid?)null, competencyIds = new[] { kit.Critical.Id } },
            block.ETag)).Expect(HttpStatusCode.OK);

        var after = (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);
        var published = (await kit.GetVersionAsync(org.Plain.Client)).Expect(HttpStatusCode.OK).Json!;
        before.Should().BeNull("nothing changed yet");
        after.Kinds().Should().Equal("MatrixSharedBlockChanged");
        after["isReference"]!.GetValue<bool>().Should().BeFalse();
        after.FirstReason()["details"]!["entries"]!.AsArray().Select(entry => entry!["id"]!.GetValue<Guid>()).Should().Equal(block.Id);
        (await org.Hr.Client.ItemsAsync()).ItemOf(kit.MatrixId).Should().BeNull("a changed block asks the head of the matrix unit alone");
        published["entries"]!["sharedBlocks"]!.AsArray().Single()!["competencyIds"]!.AsArray().Select(id => id!.GetValue<Guid>())
            .Should().Equal(kit.Critical.Id, kit.Profile.Id);
        changed.Json!["competencies"]!.AsArray().Should().HaveCount(1);
    }

    [Fact]
    public async Task Ac128_Ac130_ADraftBlockForTheMatrixUnitWrittenByTheHeadAbove_IsIncludedByTheUnitsHead_AndOneTheHeadCannotSeeIsNot()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org);
        var superior = org.DepartmentHead.Client;
        var forTeam = (await CreateAsync(superior, [kit.Critical.Id, kit.Profile.Id], org.Team.Id)).Expect(HttpStatusCode.Created);
        var forDepartment = (await CreateAsync(superior, [kit.Critical.Id, kit.Profile.Id], org.Department.Id)).Expect(HttpStatusCode.Created);

        var unseen = await kit.SaveAsync(org.TeamHead.Client, WithBlock(kit, forDepartment.Id, kit.Critical.Id, kit.Profile.Id));
        var included = await kit.SaveAsync(org.TeamHead.Client, WithBlock(kit, forTeam.Id, kit.Critical.Id, kit.Profile.Id));
        var problems = await ProblemsAsync(kit);

        forTeam.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        unseen.Status.Should().Be(HttpStatusCode.BadRequest, unseen.Body);
        unseen.FieldErrors("content").Should().NotBeEmpty();
        included.Status.Should().Be(HttpStatusCode.OK, included.Body);
        problems.Should().BeEmpty();
    }

    [Fact]
    public async Task Ac127_ABlockRequestWithNoComposition_IsRefusedAsAFieldError_OnCreationAndOnChange_AndChangesNothing()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var competency = await org.Admin.MakeCompetencyAsync();
        var block = (await CreateAsync(org.Admin, [competency.Id])).Expect(HttpStatusCode.Created);

        var created = await org.Admin.PostAsync("/api/v1/career/blocks", new { name = Scenarios.Unique("Блок"), scopeUnitId = (Guid?)null, competencyIds = (Guid[]?)null });
        var changed = await org.Admin.PutAsync(
            $"/api/v1/career/blocks/{block.Id}",
            new { name = block.Json!["name"]!.GetValue<string>(), scopeUnitId = (Guid?)null, competencyIds = (Guid[]?)null },
            block.ETag);
        var after = (await org.Admin.GetAsync($"/api/v1/career/blocks/{block.Id}")).Expect(HttpStatusCode.OK);

        foreach (var refused in new[] { created, changed })
        {
            refused.Status.Should().Be(HttpStatusCode.BadRequest, refused.Body);
            refused.FieldErrors("competencyIds").Should().NotBeEmpty(refused.Body);
        }

        after.ETag.Should().Be(block.ETag);
        after.Json!["competencies"]!.AsArray().Select(entry => entry!["id"]!.GetValue<Guid>()).Should().Equal(competency.Id);
    }

    [Fact]
    public async Task Ac127_AnUnusableCompetencyAmongSeveralAdded_RefusesTheWholeChange_AndUsableOnesAddedTogetherPass()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kept = await org.Admin.MakeCompetencyAsync();
        var first = await org.Admin.MakeCompetencyAsync();
        var second = await org.Admin.MakeCompetencyAsync();
        var draft = await org.TeamHead.Client.MakeCompetencyAsync();
        var block = (await CreateAsync(org.Admin, [kept.Id])).Expect(HttpStatusCode.Created);

        var withDraft = await ReplaceAsync(org.Admin, block, [kept.Id, first.Id, draft.Id]);
        var withMissing = await ReplaceAsync(org.Admin, block, [kept.Id, Guid.NewGuid(), first.Id]);
        var createdWithMissing = await CreateAsync(org.Admin, [first.Id, second.Id, Guid.NewGuid()]);
        var unchanged = (await org.Admin.GetAsync($"/api/v1/career/blocks/{block.Id}")).Expect(HttpStatusCode.OK);

        withDraft.Status.Should().Be(HttpStatusCode.BadRequest, withDraft.Body);
        withDraft.FieldErrors("competencyIds").Should().Equal(DraftReference);
        withMissing.Status.Should().Be(HttpStatusCode.BadRequest, withMissing.Body);
        withMissing.FieldErrors("competencyIds").Should().NotBeEmpty().And.NotContain(DraftReference);
        createdWithMissing.Status.Should().Be(HttpStatusCode.BadRequest, createdWithMissing.Body);
        createdWithMissing.FieldErrors("competencyIds").Should().NotBeEmpty();
        unchanged.ETag.Should().Be(block.ETag);

        var together = (await ReplaceAsync(org.Admin, block, [kept.Id, first.Id, second.Id])).Expect(HttpStatusCode.OK);

        together.Json!["competencies"]!.AsArray().Select(entry => entry!["id"]!.GetValue<Guid>()).Should().Equal(kept.Id, first.Id, second.Id);
    }

    private static Task<ApiResponse> ReplaceAsync(ApiClient client, ApiResponse block, Guid[] competencyIds) =>
        client.PutAsync(
            $"/api/v1/career/blocks/{block.Id}",
            new { name = block.Json!["name"]!.GetValue<string>(), scopeUnitId = (Guid?)null, competencyIds },
            block.ETag);

    private static Task<ApiResponse> CreateAsync(ApiClient client, Guid[] competencyIds, Guid? scopeUnitId = null, string? name = null) =>
        client.PostAsync("/api/v1/career/blocks", new { name = name ?? Scenarios.Unique("Блок"), scopeUnitId, competencyIds });

    private static JsonObject WithBlock(MatrixKit kit, Guid blockId, params Guid[] competencyIds)
    {
        var content = kit.Content();
        content["competencies"] = new JsonArray();
        content["sharedBlocks"] = new JsonArray(new JsonObject
        {
            ["blockId"] = blockId,
            ["competencyTypes"] = new JsonArray([.. competencyIds.Select(id => (JsonNode)new JsonObject { ["competencyId"] = id, ["type"] = id == kit.Critical.Id ? "Critical" : "Additional" })]),
        });
        content["blockWeights"] = new JsonArray(
            new JsonObject { ["levelId"] = kit.Junior.Id, ["blockId"] = blockId, ["percent"] = 100 },
            new JsonObject { ["levelId"] = kit.Senior.Id, ["blockId"] = blockId, ["percent"] = 100 });
        return content;
    }

    private static async Task<string[]> ProblemsAsync(MatrixKit kit) =>
        [.. (await kit.Author.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/completeness")).Expect(HttpStatusCode.OK).Json!["problems"]!.AsArray()
            .Select(problem => problem!["code"]!.GetValue<string>())];
}
