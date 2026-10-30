using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Who may create, read, change, approve and deactivate the entries of the catalogues that belong to no unit (directions, scales
/// and competencies): administrators and every HR keep them, a unit head proposes drafts that only their author, every HR and administrators
/// read, inactive entries are read by HR and administrators only, and a request for what the caller cannot read answers 404.
/// </summary>
public sealed class CatalogRightsTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string NotDraftMessage = "Согласовать можно только черновую запись";
    private const string DraftActivationMessage = "Черновую запись нужно согласовать";

    public static TheoryData<string> Kinds => new() { "directions", "scales", "competencies" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_NewEntry_IsActiveFromAnAdministratorOrAnyHr_ADraftFromAUnitHead_AndRefusedToAnyoneElse(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);

        var byAdmin = await catalogue.CreateAsync(org.Admin);
        var byHr = await catalogue.CreateAsync(org.Hr.Client);
        var byHrOfOtherUnit = await catalogue.CreateAsync(org.SiblingHr.Client);
        var byHead = await catalogue.CreateAsync(org.TeamHead.Client);
        var byOtherHead = await catalogue.CreateAsync(org.DepartmentHead.Client);
        var byPlain = await catalogue.CreateAsync(org.Plain.Client);

        foreach (var created in new[] { byAdmin, byHr, byHrOfOtherUnit })
        {
            created.Expect(HttpStatusCode.Created).Json!["status"]!.GetValue<string>().Should().Be("Active");
        }

        foreach (var proposed in new[] { byHead, byOtherHead })
        {
            proposed.Expect(HttpStatusCode.Created).Json!["status"]!.GetValue<string>().Should().Be("Draft");
        }

        byPlain.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_ADraft_IsReadByItsAuthorEveryHrAndAdministratorsOnly(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var draft = (await catalogue.CreateAsync(org.TeamHead.Client)).Expect(HttpStatusCode.Created);
        var name = draft.Json!["name"]!.GetValue<string>();

        foreach (var reader in new[] { org.TeamHead, org.Hr, org.SiblingHr })
        {
            (await reader.Client.GetAsync($"{catalogue.Path}/{draft.Id}")).Status.Should().Be(HttpStatusCode.OK, reader.User.Email);
            (await Names(reader.Client, catalogue.Path, name)).Should().Equal(name);
        }

        (await org.Admin.GetAsync($"{catalogue.Path}/{draft.Id}")).Status.Should().Be(HttpStatusCode.OK);
        (await Names(org.Admin, catalogue.Path, name)).Should().Equal(name);
        foreach (var stranger in new[] { org.DepartmentHead, org.SiblingHead, org.Plain })
        {
            (await stranger.Client.GetAsync($"{catalogue.Path}/{draft.Id}")).Status.Should().Be(HttpStatusCode.NotFound, stranger.User.Email);
            (await Names(stranger.Client, catalogue.Path, name)).Should().BeEmpty(stranger.User.Email);
        }
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_AnInactiveEntry_IsReadByHrAndAdministratorsOnly_WhileAnActiveOneIsReadByEveryone(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var active = (await catalogue.CreateAsync(org.Admin)).Expect(HttpStatusCode.Created);
        var inactive = (await catalogue.CreateAsync(org.Admin)).Expect(HttpStatusCode.Created);
        (await org.Admin.PostAsync($"{catalogue.Path}/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);

        foreach (var reader in new[] { org.Plain, org.TeamHead, org.DepartmentHead, org.Hr, org.SiblingHr })
        {
            (await reader.Client.GetAsync($"{catalogue.Path}/{active.Id}")).Status.Should().Be(HttpStatusCode.OK, reader.User.Email);
        }

        foreach (var reader in new[] { org.Hr, org.SiblingHr })
        {
            (await reader.Client.GetAsync($"{catalogue.Path}/{inactive.Id}")).Status.Should().Be(HttpStatusCode.OK, reader.User.Email);
        }

        foreach (var reader in new[] { org.Plain, org.TeamHead, org.DepartmentHead })
        {
            (await reader.Client.GetAsync($"{catalogue.Path}/{inactive.Id}")).Status.Should().Be(HttpStatusCode.NotFound, reader.User.Email);
            (await Names(reader.Client, catalogue.Path, inactive.Json!["name"]!.GetValue<string>())).Should().BeEmpty(reader.User.Email);
        }
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_AUnitHead_ChangesOnlyTheirOwnDraft_AndKeepersChangeAnyEntry(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var draft = (await catalogue.CreateAsync(org.TeamHead.Client)).Expect(HttpStatusCode.Created);
        var fresh = Scenarios.Unique("Правка");

        var byAuthor = await catalogue.UpdateAsync(org.TeamHead.Client, draft, fresh);
        var byOtherHead = await catalogue.UpdateAsync(org.DepartmentHead.Client, byAuthor, Scenarios.Unique("Чужая"));
        var byPlain = await catalogue.UpdateAsync(org.Plain.Client, byAuthor, Scenarios.Unique("Чужая"));
        (await org.Hr.Client.PostAsync($"{catalogue.Path}/{draft.Id}/approve", ifMatch: byAuthor.ETag)).Expect(HttpStatusCode.OK);
        var approved = (await org.Admin.GetAsync($"{catalogue.Path}/{draft.Id}")).Expect(HttpStatusCode.OK);
        var afterApproval = await catalogue.UpdateAsync(org.TeamHead.Client, approved, Scenarios.Unique("Поздно"));
        var byHr = await catalogue.UpdateAsync(org.SiblingHr.Client, approved, Scenarios.Unique("HR"));
        var byAdmin = await catalogue.UpdateAsync(org.Admin, byHr, Scenarios.Unique("Админ"));

        byAuthor.Status.Should().Be(HttpStatusCode.OK, byAuthor.Body);
        byAuthor.Json!["name"]!.GetValue<string>().Should().Be(fresh);
        byOtherHead.Status.Should().Be(HttpStatusCode.NotFound, "another head cannot read the draft");
        byPlain.Status.Should().Be(HttpStatusCode.NotFound);
        afterApproval.Status.Should().Be(HttpStatusCode.Forbidden, "an approved entry is the keepers'");
        byHr.Status.Should().Be(HttpStatusCode.OK, byHr.Body);
        byAdmin.Status.Should().Be(HttpStatusCode.OK, byAdmin.Body);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_Change_WithAStaleVersion_IsRefusedWith412(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var created = (await catalogue.CreateAsync(org.Admin)).Expect(HttpStatusCode.Created);
        var changed = (await catalogue.UpdateAsync(org.Admin, created, Scenarios.Unique("Первая"))).Expect(HttpStatusCode.OK);

        var stale = await catalogue.UpdateAsync(org.Admin, created, Scenarios.Unique("Вторая"));

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        (await org.Admin.GetAsync($"{catalogue.Path}/{created.Id}")).Json!["name"]!.GetValue<string>().Should().Be(changed.Json!["name"]!.GetValue<string>());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_Approval_IsTheKeepersOnly_MakesTheDraftActive_AndIsJournaledAsAnEventOfItsOwn(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var draft = (await catalogue.CreateAsync(org.TeamHead.Client)).Expect(HttpStatusCode.Created);
        var other = (await catalogue.CreateAsync(org.TeamHead.Client)).Expect(HttpStatusCode.Created);

        var byAuthor = await org.TeamHead.Client.PostAsync($"{catalogue.Path}/{draft.Id}/approve", ifMatch: draft.ETag);
        var byStranger = await org.DepartmentHead.Client.PostAsync($"{catalogue.Path}/{draft.Id}/approve", ifMatch: draft.ETag);
        var stale = await org.Hr.Client.PostAsync($"{catalogue.Path}/{draft.Id}/approve", ifMatch: "\"999\"");
        var byHr = (await org.Hr.Client.PostAsync($"{catalogue.Path}/{draft.Id}/approve", ifMatch: draft.ETag)).Expect(HttpStatusCode.OK);
        var again = await org.Admin.PostAsync($"{catalogue.Path}/{draft.Id}/approve", ifMatch: byHr.ETag);
        var byAdmin = (await org.Admin.PostAsync($"{catalogue.Path}/{other.Id}/approve", ifMatch: other.ETag)).Expect(HttpStatusCode.OK);

        byAuthor.Status.Should().Be(HttpStatusCode.Forbidden, "a head proposes but does not approve");
        byStranger.Status.Should().Be(HttpStatusCode.NotFound);
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        byHr.Json!["status"]!.GetValue<string>().Should().Be("Active");
        again.Status.Should().Be(HttpStatusCode.Conflict);
        again.Json!["detail"]!.GetValue<string>().Should().Be(NotDraftMessage);
        byAdmin.Json!["status"]!.GetValue<string>().Should().Be("Active");
        (await org.Admin.AuditOfRequestAsync(byHr.RequestId!)).Actions().Should().Contain($"{catalogue.EntityType}.Approved");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_Deactivation_AndReactivation_AreTheKeepersOnly_AndADraftIsActivatedByApprovalAlone(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var entry = (await catalogue.CreateAsync(org.Admin)).Expect(HttpStatusCode.Created);
        var draft = (await catalogue.CreateAsync(org.TeamHead.Client)).Expect(HttpStatusCode.Created);

        var byHead = await org.TeamHead.Client.PostAsync($"{catalogue.Path}/{draft.Id}/deactivate", ifMatch: draft.ETag);
        var byPlain = await org.Plain.Client.PostAsync($"{catalogue.Path}/{entry.Id}/deactivate", ifMatch: entry.ETag);
        var deactivated = (await org.Hr.Client.PostAsync($"{catalogue.Path}/{entry.Id}/deactivate", ifMatch: entry.ETag)).Expect(HttpStatusCode.OK);
        var reactivated = (await org.SiblingHr.Client.PostAsync($"{catalogue.Path}/{entry.Id}/activate", ifMatch: deactivated.ETag)).Expect(HttpStatusCode.OK);
        var draftActivation = await org.Hr.Client.PostAsync($"{catalogue.Path}/{draft.Id}/activate", ifMatch: draft.ETag);
        var draftDeactivated = (await org.Admin.PostAsync($"{catalogue.Path}/{draft.Id}/deactivate", ifMatch: draft.ETag)).Expect(HttpStatusCode.OK);

        byHead.Status.Should().Be(HttpStatusCode.Forbidden);
        byPlain.Status.Should().Be(HttpStatusCode.Forbidden);
        deactivated.Json!["status"]!.GetValue<string>().Should().Be("Inactive");
        reactivated.Json!["status"]!.GetValue<string>().Should().Be("Active");
        draftActivation.Status.Should().Be(HttpStatusCode.Conflict);
        draftActivation.Json!["detail"]!.GetValue<string>().Should().Be(DraftActivationMessage);
        draftDeactivated.Json!["status"]!.GetValue<string>().Should().Be("Inactive");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_EveryChangeOfAnEntry_NeedsAnIfMatchHeader(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var entry = (await catalogue.CreateAsync(org.Admin)).Expect(HttpStatusCode.Created);

        var refusals = new List<string>();
        foreach (var call in new[] { "approve", "deactivate", "activate" })
        {
            var response = await org.Admin.PostAsync($"{catalogue.Path}/{entry.Id}/{call}");
            if (response.Status != HttpStatusCode.PreconditionRequired)
            {
                refusals.Add($"{call}: {response.Code}");
            }
        }

        refusals.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac24_TheListsAreFilteredByStatus_ForWhoeverMayReadThatStatus(string kind)
    {
        var org = await fixture.OrgAsync();
        var catalogue = await CatalogueAsync(kind, org);
        var active = (await catalogue.CreateAsync(org.Admin)).Expect(HttpStatusCode.Created);
        var draft = (await catalogue.CreateAsync(org.TeamHead.Client)).Expect(HttpStatusCode.Created);
        var inactive = (await catalogue.CreateAsync(org.Admin)).Expect(HttpStatusCode.Created);
        (await org.Admin.PostAsync($"{catalogue.Path}/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);
        var names = new[] { active, draft, inactive }.Select(entry => entry.Json!["name"]!.GetValue<string>()).ToArray();

        async Task<string[]> Listed(ApiClient who, string status) =>
            [.. (await who.GetAsync($"{catalogue.Path}?status={status}&pageSize=200")).Expect(HttpStatusCode.OK).ItemNames().Where(names.Contains)];

        (await Listed(org.Hr.Client, "Active")).Should().Equal(names[0]);
        (await Listed(org.Hr.Client, "Draft")).Should().Equal(names[1]);
        (await Listed(org.Hr.Client, "Inactive")).Should().Equal(names[2]);
        (await Listed(org.Plain.Client, "Active")).Should().Equal(names[0]);
        (await Listed(org.Plain.Client, "Draft")).Should().BeEmpty();
        (await Listed(org.Plain.Client, "Inactive")).Should().BeEmpty();
        (await Listed(org.TeamHead.Client, "Draft")).Should().Equal(names[1]);
    }

    private static async Task<string[]> Names(ApiClient client, string path, string name) =>
        (await client.GetAsync($"{path}?q={Uri.EscapeDataString(name)}&pageSize=200")).Expect(HttpStatusCode.OK).ItemNames();

    private async Task<Catalogue> CatalogueAsync(string kind, CareerOrg org)
    {
        return kind switch
        {
            "directions" => new Catalogue("/api/v1/career/directions", "CareerDirection", name => new { name }),
            "scales" => new Catalogue("/api/v1/career/scales", "CompetencyScale", name => new { name, points = CareerScenarios.Points() }),
            _ => new Catalogue(
                "/api/v1/career/competencies",
                "CareerCompetency",
                name => new { name, description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)null }),
        };
    }

    private sealed record Catalogue(string Path, string EntityType, Func<string, object> Body)
    {
        public Task<ApiResponse> CreateAsync(ApiClient client) => client.PostAsync(Path, Body(Scenarios.Unique("Запись")));

        public Task<ApiResponse> UpdateAsync(ApiClient client, ApiResponse entry, string name) =>
            client.PutAsync($"{Path}/{entry.Id}", Body(name), entry.ETag);
    }
}
