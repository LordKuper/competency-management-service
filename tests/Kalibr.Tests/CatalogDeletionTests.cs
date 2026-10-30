using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Career records are deleted for good, with a journal event for every row, only when nothing refers to them; whoever may change a record deletes
/// it and the author also deletes a draft; a refusal says why and names the first five records in the way that the user can read, and counts the rest.
/// </summary>
public sealed class CatalogDeletionTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string DirectionInUse = "Область используют треки, компетенции или матрицы — удалить нельзя";
    private const string TrackInUse = "Трек используют матрицы — удалить нельзя";
    private const string LevelInUse = "Уровень используют матрицы — удалить нельзя";
    private const string CompetencyInUse = "Компетенцию используют матрицы — удалить нельзя";
    private const string ScaleInUse = "Шкалу используют компетенции — удалить нельзя";
    private const string DefaultScale = "Шкалу по умолчанию удалить нельзя";
    private const string MatrixPublished = "Матрицу с опубликованными версиями удалить нельзя";

    [Fact]
    public async Task Ac25_AnUnusedDirection_IsDeletedForGood_AndTheJournalRecordsIt()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();

        var deleted = (await org.Hr.Client.DeleteAsync($"/api/v1/career/directions/{direction.Id}", direction.ETag)).Expect(HttpStatusCode.NoContent);

        (await org.Admin.GetAsync($"/api/v1/career/directions/{direction.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
        (await org.Hr.Client.DeleteAsync($"/api/v1/career/directions/{direction.Id}", direction.ETag)).Status.Should().Be(HttpStatusCode.NotFound);
        var row = (await org.Admin.AuditOfRequestAsync(deleted.RequestId!)).Single()!;
        row["action"]!.GetValue<string>().Should().Be("CareerDirection.Deleted");
        row["entityId"]!.GetValue<string>().Should().Be(direction.Id.ToString());
        row["oldValue"]!["name"]!.GetValue<string>().Should().Be(direction.Json!["name"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("directions", HttpStatusCode.NoContent, HttpStatusCode.NoContent)]
    [InlineData("scales", HttpStatusCode.NoContent, HttpStatusCode.NoContent)]
    [InlineData("competencies", HttpStatusCode.NoContent, HttpStatusCode.NoContent)]
    [InlineData("tracks", HttpStatusCode.NotFound, HttpStatusCode.Forbidden)]
    public async Task Ac25_Deleting_IsForWhoeverMayChangeTheRecord_AndTheAuthorAlsoDeletesADraft(string kind, HttpStatusCode otherHrOnDraft, HttpStatusCode otherHrOnActive)
    {
        var org = await fixture.OrgAsync();
        var make = Maker(kind, org);
        var active = await make(org.Admin);
        var ownDraft = await make(org.TeamHead.Client);
        var othersDraft = await make(org.SiblingHead.Client);
        var path = PathOf(kind);

        var plain = await org.Plain.Client.DeleteAsync($"{path}/{active.Id}", active.ETag);
        var headOnActive = await org.TeamHead.Client.DeleteAsync($"{path}/{active.Id}", active.ETag);
        var strangerOnDraft = await org.DepartmentHead.Client.DeleteAsync($"{path}/{ownDraft.Id}", ownDraft.ETag);
        var stale = await org.Admin.DeleteAsync($"{path}/{active.Id}", "\"999\"");
        var authorOnDraft = await org.TeamHead.Client.DeleteAsync($"{path}/{ownDraft.Id}", ownDraft.ETag);
        var rootHrOnDraft = await org.Hr.Client.DeleteAsync($"{path}/{othersDraft.Id}", othersDraft.ETag);
        var siblingHrOnActive = await org.SiblingHr.Client.DeleteAsync($"{path}/{active.Id}", active.ETag);
        var siblingHrOnDraft = await org.SiblingHr.Client.DeleteAsync($"{path}/{othersDraft.Id}", othersDraft.ETag);

        plain.Status.Should().Be(HttpStatusCode.Forbidden);
        headOnActive.Status.Should().Be(HttpStatusCode.Forbidden);
        strangerOnDraft.Status.Should().Be(HttpStatusCode.NotFound, "a draft is invisible to anyone but its author, the keepers and administrators");
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        authorOnDraft.Status.Should().Be(HttpStatusCode.NoContent, authorOnDraft.Body);
        rootHrOnDraft.Status.Should().Be(otherHrOnDraft);
        siblingHrOnActive.Status.Should().Be(otherHrOnActive);
        siblingHrOnDraft.Status.Should().Be(otherHrOnDraft == HttpStatusCode.NoContent ? HttpStatusCode.NotFound : HttpStatusCode.NoContent, "the draft is deleted once, by the HR who reads it");
        (await org.Admin.GetAsync($"{path}/{ownDraft.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac25_ADirectionInUse_IsRefused_NamingTheTracksCompetenciesHistoryAndMatrices()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var history = await org.Admin.MakeCompetencyAsync(Scenarios.Unique("Была"), kit.Direction.Id);
        var other = await org.Admin.MakeDirectionAsync();
        (await org.Admin.PutAsync(
            $"/api/v1/career/competencies/{history.Id}",
            new { name = history.Json!["name"]!.GetValue<string>(), description = (string?)null, directionId = other.Id, scaleId = (Guid?)null },
            history.ETag)).Expect(HttpStatusCode.OK);

        var refused = await org.Admin.DeleteAsync($"/api/v1/career/directions/{kit.Direction.Id}", kit.Direction.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Be(DirectionInUse);
        Blockers(refused).Should().Equal(
            $"Track:{kit.Track.Json!["name"]!.GetValue<string>()}",
            $"Competency:{kit.Critical.Json!["name"]!.GetValue<string>()}",
            $"CompetencyVersion:{history.Json["name"]!.GetValue<string>()}",
            $"Matrix:{kit.Track.Json["name"]!.GetValue<string>()}");
        refused.Json["moreCount"]!.GetValue<int>().Should().Be(0);
        (await org.Admin.GetAsync($"/api/v1/career/directions/{kit.Direction.Id}")).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac25_ATrack_IsDeletedWithItsLevels_UnlessAMatrixIsBuiltOnIt()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var free = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var levels = new[] { await org.Admin.MakeLevelAsync(free.Id), await org.Admin.MakeLevelAsync(free.Id) };
        var kit = await MatrixKit.CreateAsync(org);

        var refused = await org.Admin.DeleteAsync($"/api/v1/career/tracks/{kit.Track.Id}", kit.Track.ETag);
        var deleted = (await org.Hr.Client.DeleteAsync($"/api/v1/career/tracks/{free.Id}", free.ETag)).Expect(HttpStatusCode.NoContent);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Be(TrackInUse);
        var blocker = refused.Json["blockers"]!.AsArray().Single()!;
        blocker["kind"]!.GetValue<string>().Should().Be("Matrix");
        blocker["id"]!.GetValue<Guid>().Should().Be(kit.MatrixId);
        blocker["name"]!.GetValue<string>().Should().Be(kit.Track.Json!["name"]!.GetValue<string>(), "a matrix is named by its track");
        blocker["context"]!.GetValue<string>().Should().Be(org.Team.Json!["name"]!.GetValue<string>(), "and placed by its unit");
        foreach (var level in levels)
        {
            (await org.Admin.GetAsync($"/api/v1/career/levels/{level.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
        }

        (await org.Admin.AuditOfRequestAsync(deleted.RequestId!)).Actions().Should().Equal("CareerLevel.Deleted", "CareerLevel.Deleted", "CareerTrack.Deleted");
    }

    [Fact]
    public async Task Ac25_ALevel_IsDeletedUnlessAMatrixVersionAVariantOrASnapshotNamesIt()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var inVersion = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var inVariant = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var inSnapshot = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var unused = await org.Admin.MakeLevelAsync(inVersion.Track.Id);
        (await inVariant.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await inVariant.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "Назад" })).Expect(HttpStatusCode.OK);
        (await inVariant.TransitAsync(org.TeamHead.Client, "rollback-changes")).Expect(HttpStatusCode.OK);
        var withoutJunior = inVariant.Content();
        withoutJunior["levels"]!.AsArray().RemoveAt(0);
        withoutJunior["blockWeights"]!.AsArray().RemoveAt(0);
        withoutJunior["requirements"]!.AsArray().RemoveAt(1);
        withoutJunior["requirements"]!.AsArray().RemoveAt(0);
        (await inVariant.SaveAsync(org.TeamHead.Client, withoutJunior)).Expect(HttpStatusCode.OK);
        await inSnapshot.PublishAsync();
        await fixture.Host.ExecuteAsync($"UPDATE matrix_versions SET content = '{{\"levels\":[],\"competencies\":[],\"blockWeights\":[],\"requirements\":[]}}'::jsonb WHERE id = '{inSnapshot.VersionId}'");

        var byVersion = await org.Admin.DeleteAsync($"/api/v1/career/levels/{inVersion.Junior.Id}", inVersion.Junior.ETag);
        var byVariant = await org.Admin.DeleteAsync($"/api/v1/career/levels/{inVariant.Junior.Id}", inVariant.Junior.ETag);
        var bySnapshot = await org.Admin.DeleteAsync($"/api/v1/career/levels/{inSnapshot.Junior.Id}", inSnapshot.Junior.ETag);
        var free = (await org.Admin.DeleteAsync($"/api/v1/career/levels/{unused.Id}", unused.ETag)).Expect(HttpStatusCode.NoContent);

        foreach (var refused in new[] { byVersion, byVariant, bySnapshot })
        {
            refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
            refused.Json!["detail"]!.GetValue<string>().Should().Be(LevelInUse);
            refused.Json["blockers"]!.AsArray().Select(blocker => blocker!["kind"]!.GetValue<string>()).Should().Equal("Matrix");
        }

        (await org.Admin.AuditOfRequestAsync(free.RequestId!)).Actions().Should().Equal("CareerLevel.Deleted");
    }

    [Fact]
    public async Task Ac25_ACompetency_IsDeletedWithItsHistory_UnlessAMatrixUsesIt()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var free = await org.Admin.MakeCompetencyAsync();
        var renamed = (await org.Admin.PutAsync(
            $"/api/v1/career/competencies/{free.Id}",
            new { name = Scenarios.Unique("Новое"), description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)null },
            free.ETag)).Expect(HttpStatusCode.OK);

        var refused = await org.Admin.DeleteAsync($"/api/v1/career/competencies/{kit.Critical.Id}", kit.Critical.ETag);
        var deleted = (await org.Hr.Client.DeleteAsync($"/api/v1/career/competencies/{free.Id}", renamed.ETag)).Expect(HttpStatusCode.NoContent);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Be(CompetencyInUse);
        Blockers(refused).Should().Equal($"Matrix:{kit.Track.Json!["name"]!.GetValue<string>()}");
        (await org.Admin.AuditOfRequestAsync(deleted.RequestId!)).Actions().Should().Equal("CareerCompetency.Deleted", "CompetencyVersion.Deleted", "CompetencyVersion.Deleted");
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM competency_versions WHERE competency_id = '{free.Id}'")).Should().Be(0);
    }

    [Fact]
    public async Task Ac25_AScale_IsRefusedWhileItIsTheDefaultOrCompetenciesTheirHistoryOrVersionsUseIt()
    {
        var org = await fixture.OrgAsync();
        var byCompetency = await org.Admin.MakeScaleAsync();
        var competency = await org.Admin.MakeCompetencyAsync(scaleId: byCompetency.Id);
        var byHistory = await org.Admin.MakeScaleAsync();
        var moved = await org.Admin.MakeCompetencyAsync(scaleId: byHistory.Id);
        (await org.Admin.PutAsync(
            $"/api/v1/career/competencies/{moved.Id}",
            new { name = moved.Json!["name"]!.GetValue<string>(), description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)null },
            moved.ETag)).Expect(HttpStatusCode.OK);
        var byMatrix = await org.Admin.MakeScaleAsync();
        var kit = await MatrixKit.CreateAsync(org, scaleId: byMatrix.Id);
        var unused = await org.Admin.MakeScaleAsync();
        var baseScale = (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK);

        var isDefault = await org.Admin.DeleteAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}", baseScale.ETag);
        var usedByCompetency = await org.Admin.DeleteAsync($"/api/v1/career/scales/{byCompetency.Id}", byCompetency.ETag);
        var usedByHistory = await org.Admin.DeleteAsync($"/api/v1/career/scales/{byHistory.Id}", byHistory.ETag);
        var usedByMatrix = await org.Admin.DeleteAsync($"/api/v1/career/scales/{byMatrix.Id}", byMatrix.ETag);
        var free = await org.Hr.Client.DeleteAsync($"/api/v1/career/scales/{unused.Id}", unused.ETag);

        isDefault.Status.Should().Be(HttpStatusCode.Conflict);
        isDefault.Json!["detail"]!.GetValue<string>().Should().Be(DefaultScale);
        isDefault.Json["blockers"]!.AsArray().Should().BeEmpty();
        isDefault.Json["moreCount"]!.GetValue<int>().Should().Be(0);
        usedByCompetency.Json!["detail"]!.GetValue<string>().Should().Be(ScaleInUse);
        Blockers(usedByCompetency).Should().Equal($"Competency:{competency.Json!["name"]!.GetValue<string>()}");
        Blockers(usedByHistory).Should().Equal($"CompetencyVersion:{moved.Json["name"]!.GetValue<string>()}");
        Blockers(usedByMatrix).Should().Equal(
            $"Competency:{kit.Critical.Json!["name"]!.GetValue<string>()}",
            $"Competency:{kit.Profile.Json!["name"]!.GetValue<string>()}");
        free.Status.Should().Be(HttpStatusCode.NoContent, free.Body);
    }

    [Fact]
    public async Task Ac25_AMatrix_IsDeletedWithItsVersionVariantsAndDecisions_ByTheUnitsHeadOrAnAdministrator_WhileNoVersionWasPublished()
    {
        var org = await fixture.OrgAsync();
        var byHead = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var byAdmin = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var published = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        foreach (var kit in new[] { byHead, byAdmin })
        {
            (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
            (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "Назад" })).Expect(HttpStatusCode.OK);
        }

        await published.PublishAsync();

        var etag = await byHead.ETagAsync();
        var byPlain = await org.Plain.Client.DeleteAsync($"/api/v1/career/matrices/{byHead.MatrixId}", etag);
        var byHr = await org.Hr.Client.DeleteAsync($"/api/v1/career/matrices/{byHead.MatrixId}", etag);
        var byOtherHead = await org.DepartmentHead.Client.DeleteAsync($"/api/v1/career/matrices/{byHead.MatrixId}", etag);
        var refused = await org.TeamHead.Client.DeleteAsync($"/api/v1/career/matrices/{published.MatrixId}", await published.ETagAsync());
        var deleted = (await org.TeamHead.Client.DeleteAsync($"/api/v1/career/matrices/{byHead.MatrixId}", etag)).Expect(HttpStatusCode.NoContent);
        var deletedByAdmin = (await org.Admin.DeleteAsync($"/api/v1/career/matrices/{byAdmin.MatrixId}", await byAdmin.ETagAsync())).Expect(HttpStatusCode.NoContent);

        foreach (var forbidden in new[] { byPlain, byHr, byOtherHead })
        {
            forbidden.Status.Should().Be(HttpStatusCode.Forbidden);
        }

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Be(MatrixPublished);
        (await org.Plain.Client.GetAsync($"/api/v1/career/matrices/{byHead.MatrixId}")).Status.Should().Be(HttpStatusCode.NotFound);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM matrix_versions WHERE matrix_id IN ('{byHead.MatrixId}', '{byAdmin.MatrixId}')")).Should().Be(0);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM matrix_version_decisions WHERE version_id IN ('{byHead.VersionId}', '{byAdmin.VersionId}')")).Should().Be(0);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM matrix_version_variants WHERE version_id IN ('{byHead.VersionId}', '{byAdmin.VersionId}')")).Should().Be(0);
        var actions = (await org.Admin.AuditOfRequestAsync(deleted.RequestId!)).Actions();
        actions.Should().Contain(["MatrixVariant.Deleted", "MatrixDecision.Deleted", "MatrixVersion.Deleted", "CompetencyMatrix.Deleted"], "every row is journaled");
        deletedByAdmin.Should().NotBeNull();
    }

    [Fact]
    public async Task Ac17_DeletingAMatrix_WithAStaleVersion_IsRefusedWith412_AndKeepsTheMatrixAndItsHistory()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "Назад" })).Expect(HttpStatusCode.OK);

        var stale = await org.TeamHead.Client.DeleteAsync($"/api/v1/career/matrices/{kit.MatrixId}", "\"999\"");

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        (await org.Plain.Client.GetAsync($"/api/v1/career/matrices/{kit.MatrixId}")).Status.Should().Be(HttpStatusCode.OK);
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Returned@Superior");
    }

    [Fact]
    public async Task Ac17_TheVersionOnAMatrixRow_IsTheTagOfTheVersionInWork_AndADeleteThatSendsTheTagShownBeforeALaterChange_IsRefused()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var path = $"/api/v1/career/matrices/{kit.MatrixId}";

        async Task<int?> ReadAsync() => TagOf((await org.Admin.GetAsync(path)).Expect(HttpStatusCode.OK).Json);
        async Task<int?> ListedAsync() =>
            TagOf((await org.Admin.GetAsync($"/api/v1/career/matrices?trackId={kit.Track.Id}&pageSize=200")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray().Single());

        var shown = await ListedAsync();
        shown.Should().NotBeNull("the row of the list carries the tag of the version in work");
        (await ReadAsync()).Should().Be(shown);
        $"\"{shown}\"".Should().Be(await kit.ETagAsync(), "it is the tag the version is changed and deleted with");
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "Назад" })).Expect(HttpStatusCode.OK);

        var stale = await org.TeamHead.Client.DeleteAsync(path, $"\"{shown}\"");

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        (await org.Plain.Client.GetAsync(path)).Status.Should().Be(HttpStatusCode.OK);
        var current = await ListedAsync();
        current.Should().BeGreaterThan(shown!.Value);
        $"\"{current}\"".Should().Be(await kit.ETagAsync());
        (await org.TeamHead.Client.DeleteAsync(path, $"\"{current}\"")).Expect(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Ac25_TheRefusal_NamesAtMostFiveRecordsInNameOrder_AndCountsTheOthers()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var tracks = new List<ApiResponse>();
        foreach (var letter in new[] { "Ж", "Б", "Д", "А", "Г", "В", "Е" })
        {
            tracks.Add(await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id, $"{letter}-трек"));
        }

        var refused = await org.Admin.DeleteAsync($"/api/v1/career/directions/{direction.Id}", direction.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        Blockers(refused).Should().Equal("Track:А-трек", "Track:Б-трек", "Track:В-трек", "Track:Г-трек", "Track:Д-трек");
        refused.Json!["moreCount"]!.GetValue<int>().Should().Be(2);
    }

    [Fact]
    public async Task Ac25_TheRefusal_CountsTheRecordsTheUserCannotRead_WithoutNamingThem()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var visible = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id, Scenarios.Unique("Видимый"));
        var secret = Scenarios.Unique("Секретный");
        var draft = (await org.TeamHead.Client.PostAsync("/api/v1/career/tracks", new { name = secret, ownerOrgUnitId = org.Team.Id, directionId = direction.Id })).Expect(HttpStatusCode.Created);

        var byOtherHr = await org.SiblingHr.Client.DeleteAsync($"/api/v1/career/directions/{direction.Id}", direction.ETag);
        var byAdmin = await org.Admin.DeleteAsync($"/api/v1/career/directions/{direction.Id}", direction.ETag);

        byOtherHr.Status.Should().Be(HttpStatusCode.Conflict);
        Blockers(byOtherHr).Should().Equal($"Track:{visible.Json!["name"]!.GetValue<string>()}");
        byOtherHr.Json!["moreCount"]!.GetValue<int>().Should().Be(1, "the draft track of a unit the HR does not answer for is counted but not named");
        byOtherHr.Body.Should().NotContain(secret);
        byOtherHr.Body.Should().NotContain(draft.Id.ToString());
        Blockers(byAdmin).Should().BeEquivalentTo([$"Track:{visible.Json["name"]!.GetValue<string>()}", $"Track:{secret}"]);
        byAdmin.Json!["moreCount"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task Ac25_Deleting_NeedsAnIfMatchHeaderOnEveryRecord()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var kit = await MatrixKit.CreateAsync(org);
        var paths = new[]
        {
            $"/api/v1/career/directions/{direction.Id}",
            $"/api/v1/career/tracks/{kit.Track.Id}",
            $"/api/v1/career/levels/{kit.Junior.Id}",
            $"/api/v1/career/competencies/{kit.Critical.Id}",
            $"/api/v1/career/scales/{CareerScenarios.BaseScaleId}",
            $"/api/v1/career/matrices/{kit.MatrixId}",
        };

        var wrong = new List<string>();
        foreach (var path in paths)
        {
            var response = await org.Admin.DeleteAsync(path);
            if (response.Status != HttpStatusCode.PreconditionRequired)
            {
                wrong.Add($"{path}: {response.Code}");
            }
        }

        wrong.Should().BeEmpty();
    }

    private static int? TagOf(JsonNode? matrix) => matrix?["inWorkVersion"]?["version"]?.GetValue<int>();

    private static string[] Blockers(ApiResponse refusal) =>
        [.. refusal.Json!["blockers"]!.AsArray().Select(blocker => $"{blocker!["kind"]!.GetValue<string>()}:{blocker["name"]!.GetValue<string>()}")];

    private static string PathOf(string kind) => $"/api/v1/career/{kind}";

    private static Func<ApiClient, Task<ApiResponse>> Maker(string kind, CareerOrg org) => kind switch
    {
        "directions" => client => client.MakeDirectionAsync(),
        "scales" => client => client.MakeScaleAsync(),
        "competencies" => async client => await client.MakeCompetencyAsync(),
        _ => async client =>
        {
            var unitId = client == org.SiblingHead.Client ? org.Sibling.Id : org.Team.Id;
            var directionId = (await org.Admin.MakeDirectionAsync()).Id;
            return client == org.Admin ? await client.MakeActiveTrackAsync(unitId, directionId) : await client.MakeTrackAsync(unitId, directionId);
        },
    };
}
