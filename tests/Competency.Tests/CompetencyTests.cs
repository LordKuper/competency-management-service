using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The competency catalogue: a competency refers to a category, a direction or none, a type and a scale, a request without a scale gets the
/// default one, every saved change adds a version to the competency's history, and lists are filtered by the competency's columns.
/// </summary>
public sealed class CompetencyTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string UnavailableMessage = "Запись не найдена или недоступна";

    [Fact]
    public async Task Ac12_NewCompetency_WithoutAScale_GetsTheDefaultScale_AndWithOneGetsIt()
    {
        var org = await fixture.OrgAsync();
        var categoryId = await org.Admin.CategoryIdAsync();
        var scale = await org.Admin.MakeScaleAsync();

        var byDefault = await org.Admin.MakeCompetencyAsync(categoryId);
        var explicitScale = await org.Admin.MakeCompetencyAsync(categoryId, scaleId: scale.Id);
        var changed = (await org.Admin.PutAsync($"/api/v1/career/competencies/{explicitScale.Id}", Body(Scenarios.Unique("Навык"), categoryId, null, scaleId: null), explicitScale.ETag)).Expect(HttpStatusCode.OK);

        byDefault.Json!["scaleId"]!.GetValue<string>().Should().Be(CareerScenarios.BaseScaleId);
        explicitScale.Json!["scaleId"]!.GetValue<Guid>().Should().Be(scale.Id);
        changed.Json!["scaleId"]!.GetValue<string>().Should().Be(CareerScenarios.BaseScaleId, "a change that names no scale puts the default one");
    }

    [Fact]
    public async Task Ac12_Competency_KeepsItsFields_AndShowsItsVersionNumber()
    {
        var org = await fixture.OrgAsync();
        var categoryId = await org.Admin.CategoryIdAsync();
        var direction = await org.Admin.MakeDirectionAsync();

        var created = (await org.Admin.PostAsync(
            "/api/v1/career/competencies",
            new { name = "  Проектирование  ", description = "  Описание  ", categoryId, directionId = direction.Id, type = "Critical", scaleId = (Guid?)null })).Expect(HttpStatusCode.Created);
        var blankDescription = (await org.Admin.PutAsync(
            $"/api/v1/career/competencies/{created.Id}",
            new { name = "Проектирование", description = "   ", categoryId, directionId = (Guid?)null, type = "Additional", scaleId = (Guid?)null },
            created.ETag)).Expect(HttpStatusCode.OK);

        created.Json!["name"]!.GetValue<string>().Should().Be("Проектирование");
        created.Json["description"]!.GetValue<string>().Should().Be("Описание");
        created.Json["categoryId"]!.GetValue<Guid>().Should().Be(categoryId);
        created.Json["directionId"]!.GetValue<Guid>().Should().Be(direction.Id);
        created.Json["type"]!.GetValue<string>().Should().Be("Critical");
        created.Json["versionNumber"]!.GetValue<int>().Should().Be(1);
        created.Json["createdAt"].Should().NotBeNull();
        blankDescription.Json!["description"].Should().BeNull("an empty description is stored as none");
        blankDescription.Json["directionId"].Should().BeNull();
        blankDescription.Json["type"]!.GetValue<string>().Should().Be("Additional");
        blankDescription.Json["versionNumber"]!.GetValue<int>().Should().Be(2);
    }

    [Fact]
    public async Task Ac12_Competency_IsValidated()
    {
        var org = await fixture.OrgAsync();
        var categoryId = await org.Admin.CategoryIdAsync();

        var blank = await org.Admin.PostAsync("/api/v1/career/competencies", Body("  ", categoryId, null));
        var longName = await org.Admin.PostAsync("/api/v1/career/competencies", Body(new string('я', 201), categoryId, null));
        var longDescription = await org.Admin.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), categoryId, null, description: new string('я', 4001)));
        var longestDescription = await org.Admin.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), categoryId, null, description: new string('я', 4000)));
        var unknownType = await org.Admin.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), categoryId, null, type: "Unknown"));

        blank.FieldErrors("name").Should().Equal("Укажите название");
        longName.FieldErrors("name").Should().Equal("Не длиннее 200 символов");
        longDescription.FieldErrors("description").Should().Equal("Не длиннее 4000 символов");
        longestDescription.Status.Should().Be(HttpStatusCode.Created, longestDescription.Body);
        unknownType.Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ac12_Competency_RefusesANumberThatNamesNoType_OnCreationAndOnChange()
    {
        var org = await fixture.OrgAsync();
        var categoryId = await org.Admin.CategoryIdAsync();
        var competency = await org.Admin.MakeCompetencyAsync(categoryId);

        var created = await org.Admin.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), categoryId, null, type: 999));
        var changed = await org.Admin.PutAsync($"/api/v1/career/competencies/{competency.Id}", Body(Scenarios.Unique("Навык"), categoryId, null, type: 999), competency.ETag);

        foreach (var refused in new[] { created, changed })
        {
            refused.Status.Should().Be(HttpStatusCode.BadRequest, refused.Body);
            refused.FieldErrors("type").Should().Equal("Укажите допустимый тип");
        }

        (await org.Admin.GetAsync($"/api/v1/career/competencies/{competency.Id}")).Json!["type"]!.GetValue<string>().Should().Be("Profile");
    }

    [Fact]
    public async Task Ac12_Competency_CanRefer_OnlyToActiveEntriesAndToDraftsOfItsAuthor()
    {
        var org = await fixture.OrgAsync();
        var categoryId = await org.Admin.CategoryIdAsync();
        var inactiveCategory = await org.Admin.MakeCategoryAsync();
        (await org.Admin.PostAsync($"/api/v1/career/categories/{inactiveCategory.Id}/deactivate", ifMatch: inactiveCategory.ETag)).Expect(HttpStatusCode.OK);
        var othersDraft = (await org.SiblingHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Чужое") })).Expect(HttpStatusCode.Created);
        var ownDraft = (await org.TeamHead.Client.PostAsync("/api/v1/career/categories", new { name = Scenarios.Unique("Своя") })).Expect(HttpStatusCode.Created);
        var othersScale = (await org.SiblingHead.Client.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Чужая"), points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);

        var inactive = await org.Admin.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), inactiveCategory.Id, null));
        var unknownCategory = await org.Admin.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), Guid.NewGuid(), null));
        var foreignDirection = await org.TeamHead.Client.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), categoryId, othersDraft.Id));
        var foreignScale = await org.TeamHead.Client.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), categoryId, null, scaleId: othersScale.Id));
        var ownCategory = await org.TeamHead.Client.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Навык"), ownDraft.Id, null));

        inactive.FieldErrors("categoryId").Should().Equal(UnavailableMessage);
        unknownCategory.FieldErrors("categoryId").Should().Equal(UnavailableMessage);
        foreignDirection.FieldErrors("directionId").Should().Equal(UnavailableMessage);
        foreignScale.FieldErrors("scaleId").Should().Equal(UnavailableMessage);
        ownCategory.Status.Should().Be(HttpStatusCode.Created, ownCategory.Body);
    }

    [Fact]
    public async Task Ac12_EverySavedChange_AddsAVersionToTheHistory_ListedLatestFirst()
    {
        var org = await fixture.OrgAsync();
        var categoryId = await org.Admin.CategoryIdAsync();
        var created = (await org.TeamHead.Client.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Первое"), categoryId, null))).Expect(HttpStatusCode.Created);
        var renamed = (await org.TeamHead.Client.PutAsync($"/api/v1/career/competencies/{created.Id}", Body(Scenarios.Unique("Второе"), categoryId, null), created.ETag)).Expect(HttpStatusCode.OK);
        var approved = (await org.Hr.Client.PostAsync($"/api/v1/career/competencies/{created.Id}/approve", ifMatch: renamed.ETag)).Expect(HttpStatusCode.OK);
        var deactivated = (await org.Hr.Client.PostAsync($"/api/v1/career/competencies/{created.Id}/deactivate", ifMatch: approved.ETag)).Expect(HttpStatusCode.OK);

        var versions = (await org.Hr.Client.GetAsync($"/api/v1/career/competencies/{created.Id}/versions")).Expect(HttpStatusCode.OK).Json!.AsArray();

        versions.Select(version => version!["number"]!.GetValue<int>()).Should().Equal(4, 3, 2, 1);
        versions.Select(version => version!["status"]!.GetValue<string>()).Should().Equal("Inactive", "Active", "Draft", "Draft");
        versions[3]!["name"]!.GetValue<string>().Should().Be(created.Json!["name"]!.GetValue<string>());
        versions[2]!["name"]!.GetValue<string>().Should().Be(renamed.Json!["name"]!.GetValue<string>());
        versions[3]!["changedById"]!.GetValue<Guid>().Should().Be(org.TeamHead.Id);
        versions[1]!["changedById"]!.GetValue<Guid>().Should().Be(org.Hr.Id);
        versions[3]!["changedByName"]!.GetValue<string>().Should().NotBeNullOrEmpty();
        versions[0]!["changedAt"].Should().NotBeNull();
        deactivated.Json!["versionNumber"]!.GetValue<int>().Should().Be(4);
    }

    [Fact]
    public async Task Ac12_TheHistory_IsReadByWhoeverReadsTheCompetency_AndNotFoundForTheRest()
    {
        var org = await fixture.OrgAsync();
        var categoryId = await org.Admin.CategoryIdAsync();
        var draft = (await org.TeamHead.Client.PostAsync("/api/v1/career/competencies", Body(Scenarios.Unique("Скрытое"), categoryId, null))).Expect(HttpStatusCode.Created);
        var active = await org.Admin.MakeCompetencyAsync(categoryId);

        (await org.TeamHead.Client.GetAsync($"/api/v1/career/competencies/{draft.Id}/versions")).Status.Should().Be(HttpStatusCode.OK);
        (await org.SiblingHr.Client.GetAsync($"/api/v1/career/competencies/{draft.Id}/versions")).Status.Should().Be(HttpStatusCode.OK);
        (await org.Plain.Client.GetAsync($"/api/v1/career/competencies/{draft.Id}/versions")).Status.Should().Be(HttpStatusCode.NotFound);
        (await org.DepartmentHead.Client.GetAsync($"/api/v1/career/competencies/{draft.Id}/versions")).Status.Should().Be(HttpStatusCode.NotFound);
        (await org.Plain.Client.GetAsync($"/api/v1/career/competencies/{active.Id}/versions")).Status.Should().Be(HttpStatusCode.OK);
        (await org.Plain.Client.GetAsync($"/api/v1/career/competencies/{Guid.NewGuid()}/versions")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac24_Competencies_AreFilteredByCategoryDirectionTypeScaleAndStatus()
    {
        var org = await fixture.OrgAsync();
        var category = await org.Admin.CategoryIdAsync();
        var otherCategory = await org.Admin.MakeCategoryAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var scale = await org.Admin.MakeScaleAsync();
        var token = Scenarios.Unique("ф")[2..];
        var plain = await org.Admin.MakeCompetencyAsync(category, $"Базовая {token}");
        var critical = await org.Admin.MakeCompetencyAsync(otherCategory.Id, $"Критичная {token}", direction.Id, "Critical", scale.Id);
        var additional = await org.Admin.MakeCompetencyAsync(category, $"Дополнительная {token}", direction.Id, "Additional");
        (await org.Admin.PostAsync($"/api/v1/career/competencies/{additional.Id}/deactivate", ifMatch: additional.ETag)).Expect(HttpStatusCode.OK);

        (await IdsAsync(org.Admin, token, $"categoryId={otherCategory.Id}")).Should().Equal(critical.Id.ToString());
        (await IdsAsync(org.Admin, token, $"directionId={direction.Id}")).Should().BeEquivalentTo([critical.Id.ToString(), additional.Id.ToString()]);
        (await IdsAsync(org.Admin, token, "type=Critical")).Should().Equal(critical.Id.ToString());
        (await IdsAsync(org.Admin, token, "type=Additional")).Should().Equal(additional.Id.ToString());
        (await IdsAsync(org.Admin, token, $"scaleId={scale.Id}")).Should().Equal(critical.Id.ToString());
        (await IdsAsync(org.Admin, token, "status=Inactive")).Should().Equal(additional.Id.ToString());
        (await IdsAsync(org.Admin, token, $"status=Active&categoryId={category}")).Should().Equal(plain.Id.ToString());
        (await IdsAsync(org.Plain.Client, token, string.Empty)).Should().BeEquivalentTo([plain.Id.ToString(), critical.Id.ToString()], "a user does not read the inactive one");
    }

    [Fact]
    public async Task Ac24_Competency_NamesItsCategoryItsDirectionAndItsScale_InTheListAndOnItsOwn_AndAfterAChange()
    {
        var org = await fixture.OrgAsync();
        var category = await org.Admin.MakeCategoryAsync();
        var otherCategory = await org.Admin.MakeCategoryAsync();
        var draftCategory = (await org.TeamHead.Client.PostAsync("/api/v1/career/categories", new { name = Scenarios.Unique("Своя") })).Expect(HttpStatusCode.Created);
        var direction = await org.Admin.MakeDirectionAsync();
        var scale = await org.Admin.MakeScaleAsync();
        var baseScale = (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK);
        var token = Scenarios.Unique("ф")[2..];
        var full = await org.Admin.MakeCompetencyAsync(category.Id, $"Полная {token}", direction.Id, scaleId: scale.Id);
        var sameNames = await org.Admin.MakeCompetencyAsync(category.Id, $"Те же названия {token}", direction.Id, scaleId: scale.Id);
        var general = (await org.TeamHead.Client.PostAsync("/api/v1/career/competencies", Body($"Общая {token}", draftCategory.Id, null))).Expect(HttpStatusCode.Created);
        var expected = new Dictionary<string, string>
        {
            [Name(full)] = Names(category, direction, scale),
            [Name(sameNames)] = Names(category, direction, scale),
            [Name(general)] = Names(draftCategory, null, baseScale),
        };

        var listed = (await org.Admin.GetAsync($"/api/v1/career/competencies?q={Uri.EscapeDataString(token)}&pageSize=200")).Expect(HttpStatusCode.OK);
        var read = new Dictionary<string, string>();
        foreach (var competency in new[] { full, sameNames, general })
        {
            read[Name(competency)] = NamesOf((await org.Admin.GetAsync($"/api/v1/career/competencies/{competency.Id}")).Expect(HttpStatusCode.OK).Json);
        }

        var moved = (await org.Admin.PutAsync($"/api/v1/career/competencies/{sameNames.Id}", Body(Name(sameNames), otherCategory.Id, null), sameNames.ETag)).Expect(HttpStatusCode.OK);
        var none = (await org.Admin.GetAsync($"/api/v1/career/competencies?q={Uri.EscapeDataString(Scenarios.Unique("нет"))}&pageSize=200")).Expect(HttpStatusCode.OK);

        using var scope = new AssertionScope();
        listed.Json!["items"]!.AsArray().ToDictionary(item => item!["name"]!.GetValue<string>(), NamesOf).Should().BeEquivalentTo(expected, "the list names them on one page");
        read.Should().BeEquivalentTo(expected, "a competency read on its own names them the same");
        new[] { full, sameNames, general }.ToDictionary(Name, competency => NamesOf(competency.Json)).Should().BeEquivalentTo(expected, "the response to the creation names them");
        NamesOf(moved.Json).Should().Be(Names(otherCategory, null, baseScale), "the response to a change names what the competency refers to now");
        none.Json!["items"]!.AsArray().Should().BeEmpty("a page with no competency asks for no names");
    }

    [Fact]
    public async Task Ac23_Categories_StartWithTheGeneralOnes_ReadableByEveryone()
    {
        var org = await fixture.OrgAsync();

        var categories = (await org.Plain.Client.GetAsync("/api/v1/career/categories?pageSize=200")).Expect(HttpStatusCode.OK);

        categories.Json!["total"]!.GetValue<int>().Should().BeGreaterThanOrEqualTo(12);
        categories.Json["items"]!.AsArray().Select(item => item!["status"]!.GetValue<string>()).Should().OnlyContain(status => status == "Active");
    }

    private static object Body(string name, Guid categoryId, Guid? directionId, string? description = null, object? type = null, Guid? scaleId = null) =>
        new { name, description, categoryId, directionId, type = type ?? "Profile", scaleId };

    private static string Name(ApiResponse entry) => entry.Json!["name"]!.GetValue<string>();

    private static string Names(ApiResponse category, ApiResponse? direction, ApiResponse scale) =>
        $"{Name(category)} | {(direction is null ? "none" : Name(direction))} | {Name(scale)}";

    private static string NamesOf(JsonNode? competency) => string.Join(
        " | ",
        new[] { "categoryName", "directionName", "scaleName" }.Select(field =>
            competency!.AsObject().TryGetPropertyValue(field, out var value) ? value?.GetValue<string>() ?? "none" : "missing"));

    private static async Task<string[]> IdsAsync(ApiClient client, string token, string query) =>
        (await client.GetAsync($"/api/v1/career/competencies?q={Uri.EscapeDataString(token)}&{query}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();
}
