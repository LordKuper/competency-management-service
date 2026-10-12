using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The names of catalogue entries are unique regardless of letter case and edge whitespace among the entries that are not drafts and the
/// drafts of the same author, so a draft nobody else can read never blocks a name, and the name is checked again when a draft is approved.
/// </summary>
public sealed class CatalogNameTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string DuplicateMessage = "Запись с таким названием уже есть";

    public static TheoryData<string> Kinds => new() { "directions", "categories", "scales", "competencies" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_Name_IsUniqueAmongTheEntriesThatAreNotDrafts_RegardlessOfCaseAndEdgeWhitespace_OnCreateAndRename(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var name = Scenarios.Unique("Название");
        var first = (await catalogue.CreateAsync(org.Admin, name)).Expect(HttpStatusCode.Created);
        var other = (await catalogue.CreateAsync(org.Admin, Scenarios.Unique("Другое"))).Expect(HttpStatusCode.Created);

        var byHr = await catalogue.CreateAsync(org.Hr.Client, $"  {name.ToUpperInvariant()}  ");
        var byHead = await catalogue.CreateAsync(org.TeamHead.Client, name.ToLowerInvariant());
        var renamed = await catalogue.UpdateAsync(org.Admin, other, $" {name} ");
        var ownName = await catalogue.UpdateAsync(org.Admin, first, name.ToUpperInvariant());

        foreach (var refused in new[] { byHr, byHead, renamed })
        {
            refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
            refused.FieldErrors("name").Should().Equal(DuplicateMessage);
        }

        ownName.Status.Should().Be(HttpStatusCode.OK, "an entry may change the case of its own name");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_DraftsOfDifferentAuthors_DoNotBlockEachOther_AndTheSecondCannotBeApprovedUnderTheTakenName(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var name = Scenarios.Unique("Черновик");

        var fromTeam = (await catalogue.CreateAsync(org.TeamHead.Client, name)).Expect(HttpStatusCode.Created);
        var fromSibling = (await catalogue.CreateAsync(org.SiblingHead.Client, name.ToUpperInvariant())).Expect(HttpStatusCode.Created);
        var again = await catalogue.CreateAsync(org.TeamHead.Client, $" {name} ");
        var approved = await org.Hr.Client.PostAsync($"{catalogue.Path}/{fromTeam.Id}/approve", ifMatch: fromTeam.ETag);
        var refused = await org.Hr.Client.PostAsync($"{catalogue.Path}/{fromSibling.Id}/approve", ifMatch: fromSibling.ETag);

        again.Status.Should().Be(HttpStatusCode.Conflict, "a draft competes with the author's other drafts");
        again.FieldErrors("name").Should().Equal(DuplicateMessage);
        approved.Status.Should().Be(HttpStatusCode.OK, approved.Body);
        refused.Status.Should().Be(HttpStatusCode.Conflict, "the approved entry now holds the name");
        refused.FieldErrors("name").Should().Equal(DuplicateMessage);
        (await org.Admin.GetAsync($"{catalogue.Path}/{fromSibling.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Draft");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_ADraftLeavingDraftStatusByDeactivation_CompetesLikeAnApprovedEntry(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var name = Scenarios.Unique("Закрытый");
        var fromTeam = (await catalogue.CreateAsync(org.TeamHead.Client, name)).Expect(HttpStatusCode.Created);
        var fromSibling = (await catalogue.CreateAsync(org.SiblingHead.Client, name)).Expect(HttpStatusCode.Created);
        (await org.Admin.PostAsync($"{catalogue.Path}/{fromTeam.Id}/deactivate", ifMatch: fromTeam.ETag)).Expect(HttpStatusCode.OK);

        var deactivated = await org.Admin.PostAsync($"{catalogue.Path}/{fromSibling.Id}/deactivate", ifMatch: fromSibling.ETag);
        var created = await catalogue.CreateAsync(org.Hr.Client, name);

        deactivated.Status.Should().Be(HttpStatusCode.Conflict, "the first draft left drafts, so the name is taken for everyone");
        created.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Ac10_CompetencyName_IsUniqueWithinItsDirectionAndAmongGeneralCompetencies_ButNotAcross()
    {
        var org = await fixture.OrgAsync();
        var categoryId = await org.Admin.CategoryIdAsync();
        var first = await org.Admin.MakeDirectionAsync();
        var second = await org.Admin.MakeDirectionAsync();
        var name = Scenarios.Unique("Навык");
        await org.Admin.MakeCompetencyAsync(categoryId, name, first.Id);
        await org.Admin.MakeCompetencyAsync(categoryId, name);

        var sameDirection = await org.Admin.PostAsync("/api/v1/career/competencies", Competency(name.ToUpperInvariant(), categoryId, first.Id));
        var sameGeneral = await org.Admin.PostAsync("/api/v1/career/competencies", Competency(name.ToUpperInvariant(), categoryId, null));
        var otherDirection = await org.Admin.PostAsync("/api/v1/career/competencies", Competency(name, categoryId, second.Id));

        sameDirection.Status.Should().Be(HttpStatusCode.Conflict);
        sameGeneral.Status.Should().Be(HttpStatusCode.Conflict);
        otherDirection.Status.Should().Be(HttpStatusCode.Created, otherDirection.Body);
    }

    private static object Competency(string name, Guid categoryId, Guid? directionId) =>
        new { name, description = (string?)null, categoryId, directionId, type = "Profile", scaleId = (Guid?)null };

    private static async Task<Catalogue> CatalogueAsync(string kind, CareerOrg org)
    {
        var categoryId = await org.Admin.CategoryIdAsync();
        return kind switch
        {
            "directions" => new Catalogue("/api/v1/career/directions", name => new { name }),
            "categories" => new Catalogue("/api/v1/career/categories", name => new { name }),
            "scales" => new Catalogue("/api/v1/career/scales", name => new { name, points = CareerScenarios.Points() }),
            _ => new Catalogue("/api/v1/career/competencies", name => Competency(name, categoryId, null)),
        };
    }

    private sealed record Catalogue(string Path, Func<string, object> Body)
    {
        public Task<ApiResponse> CreateAsync(ApiClient client, string name) => client.PostAsync(Path, Body(name));

        public Task<ApiResponse> UpdateAsync(ApiClient client, ApiResponse entry, string name) =>
            client.PutAsync($"{Path}/{entry.Id}", Body(name), entry.ETag);
    }
}
