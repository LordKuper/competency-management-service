using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Every response that describes a catalogue entry says whether its reader may change and delete it now (<c>canEdit</c>, <c>canDelete</c>), so the
/// client offers exactly what the server allows: keepers do both, the head of the unit does both to their own draft, the author of a draft deletes it
/// even after losing the role, and a superior approver who reads a draft only through the approval route may do neither.
/// </summary>
public sealed class CatalogEntryRightsTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private static readonly Flags Both = new(true, true);

    private static readonly Flags Neither = new(false, false);

    public static TheoryData<string> Kinds => new() { "directions", "scales", "competencies", "tracks", "levels" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac19_AnEntry_TellsEachReaderWhetherTheyMayEditAndDeleteIt_InTheListAndOnItsOwn(string kind)
    {
        var org = await fixture.OrgAsync();
        var entries = await MakeAsync(kind, org);
        var ofUnit = kind is "tracks" or "levels";
        var readers = new (string Label, ApiClient Client, Flags? OnActive, Flags? OnDraft)[]
        {
            ("administrator", org.Admin, Both, Both),
            ("HR of the unit", org.Hr.Client, Both, Both),
            ("HR of another unit", org.SiblingHr.Client, ofUnit ? Neither : Both, ofUnit ? null : Both),
            ("head of the unit, the author", org.TeamHead.Client, Neither, Both),
            ("head of the unit above", org.DepartmentHead.Client, Neither, null),
            ("user with no role", org.Plain.Client, Neither, null),
        };

        var wrong = new List<string>();
        foreach (var (label, client, onActive, onDraft) in readers)
        {
            foreach (var (which, entry, expected) in new[] { ("active", entries.Active, onActive), ("draft", entries.Draft, onDraft) })
            {
                var single = await client.GetAsync($"{PathOf(kind)}/{entry.Id}");
                var listed = await ListedAsync(client, kind, entry);
                var singleNode = single.Status == HttpStatusCode.OK ? single.Json : null;
                foreach (var (how, actual) in new[] { ("alone", singleNode), ("in the list", listed) })
                {
                    if (Describe(actual) != Describe(expected))
                    {
                        wrong.Add($"{label}, {which} entry {how}: expected {Describe(expected)}, got {Describe(actual)}");
                    }
                }
            }
        }

        foreach (var (what, created) in new[] { ("the administrator's creation", entries.Active), ("the head's creation of their draft", entries.Draft) })
        {
            if (Describe(created.Json) != Describe(Both))
            {
                wrong.Add($"{what}: expected {Describe(Both)}, got {Describe(created.Json)}");
            }
        }

        wrong.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac25_TheAuthorOfADraft_WhoNoLongerHeadsTheUnit_CannotEditIt_ButDeletesIt_AsTheFlagsSay(string kind)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var draft = (await MakeAsync(kind, org)).Draft;
        var successor = await org.Admin.CreateEmployeeAsync(org.Team.Id);
        (await org.Admin.SetHeadAsync(org.Team, successor.Id)).Expect(HttpStatusCode.OK);
        var author = org.TeamHead.Client;

        var single = (await author.GetAsync($"{PathOf(kind)}/{draft.Id}")).Expect(HttpStatusCode.OK);
        var listed = await ListedAsync(author, kind, draft);
        var edited = await author.PutAsync($"{PathOf(kind)}/{draft.Id}", UpdateBody(kind, draft), single.ETag);
        var deleted = await author.DeleteAsync($"{PathOf(kind)}/{draft.Id}", single.ETag);

        Describe(single.Json).Should().Be(Describe(new Flags(false, true)));
        Describe(listed).Should().Be(Describe(new Flags(false, true)));
        edited.Status.Should().Be(HttpStatusCode.Forbidden, edited.Body);
        deleted.Status.Should().Be(HttpStatusCode.NoContent, deleted.Body);
        (await org.Admin.GetAsync($"{PathOf(kind)}/{draft.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac19_ADraftOfAVersionInWork_ReadThroughTheApprovalRoute_OffersTheSuperiorApproverNeitherEditNorDelete()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var scale = (await org.TeamHead.Client.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Своя шкала"), points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);
        var kit = await (await MatrixKit.CreateAsync(org, scaleId: scale.Id, competencyAuthor: org.TeamHead.Client)).PrepareAsync();
        var superior = org.DepartmentHead.Client;

        foreach (var (kind, entry) in new[] { ("scales", scale), ("competencies", kit.Critical) })
        {
            var single = (await superior.GetAsync($"{PathOf(kind)}/{entry.Id}")).Expect(HttpStatusCode.OK);
            var listed = await ListedAsync(superior, kind, entry);
            var edited = await superior.PutAsync($"{PathOf(kind)}/{entry.Id}", UpdateBody(kind, entry), single.ETag);
            var deleted = await superior.DeleteAsync($"{PathOf(kind)}/{entry.Id}", single.ETag);
            var byAuthor = (await org.TeamHead.Client.GetAsync($"{PathOf(kind)}/{entry.Id}")).Expect(HttpStatusCode.OK);

            Describe(single.Json).Should().Be(Describe(Neither), kind);
            Describe(listed).Should().Be(Describe(Neither), kind);
            edited.Status.Should().Be(HttpStatusCode.Forbidden, kind);
            deleted.Status.Should().Be(HttpStatusCode.Forbidden, kind);
            Describe(byAuthor.Json).Should().Be(Describe(Both), kind);
        }
    }

    private static string PathOf(string kind) => $"/api/v1/career/{kind}";

    private static string Describe(Flags? flags) => flags is null ? "not readable" : $"edit={flags.Edit} delete={flags.Delete}";

    private static string Describe(JsonNode? entry) => entry is null
        ? "not readable"
        : $"edit={entry["canEdit"]?.GetValue<bool>().ToString() ?? "missing"} delete={entry["canDelete"]?.GetValue<bool>().ToString() ?? "missing"}";

    private static async Task<JsonNode?> ListedAsync(ApiClient who, string kind, ApiResponse entry)
    {
        var query = kind == "levels"
            ? $"trackId={entry.Json!["trackId"]!.GetValue<string>()}"
            : $"q={Uri.EscapeDataString(entry.Json!["name"]!.GetValue<string>())}";
        var page = (await who.GetAsync($"{PathOf(kind)}?{query}&pageSize=200")).Expect(HttpStatusCode.OK);
        return page.Json!["items"]!.AsArray().FirstOrDefault(item => item!["id"]!.GetValue<string>() == entry.Id.ToString());
    }

    private static object UpdateBody(string kind, ApiResponse entry) => kind switch
    {
        "scales" => new { name = Scenarios.Unique("Правка"), points = CareerScenarios.Points() },
        "competencies" => new
        {
            name = Scenarios.Unique("Правка"),
            description = (string?)null,
            directionId = (Guid?)null,
            scaleId = entry.Json!["scaleId"]!.GetValue<Guid>(),
        },
        _ => new { name = Scenarios.Unique("Правка") },
    };

    private static async Task<Entries> MakeAsync(string kind, CareerOrg org)
    {
        var admin = org.Admin;
        var head = org.TeamHead.Client;
        switch (kind)
        {
            case "directions":
                return new Entries(await admin.MakeDirectionAsync(), await head.MakeDirectionAsync());
            case "scales":
                return new Entries(await admin.MakeScaleAsync(), await head.MakeScaleAsync());
            case "competencies":
                return new Entries(await admin.MakeCompetencyAsync(), await head.MakeCompetencyAsync());
            case "tracks":
                var direction = await admin.MakeDirectionAsync();
                return new Entries(await admin.MakeActiveTrackAsync(org.Team.Id, direction.Id), await head.MakeTrackAsync(org.Team.Id, direction.Id));
            default:
                var levelDirection = await admin.MakeDirectionAsync();
                var activeTrack = await admin.MakeActiveTrackAsync(org.Team.Id, levelDirection.Id);
                var draftTrack = await head.MakeTrackAsync(org.Team.Id, levelDirection.Id);
                return new Entries(await admin.MakeLevelAsync(activeTrack.Id), await head.MakeLevelAsync(draftTrack.Id));
        }
    }

    private sealed record Flags(bool Edit, bool Delete);

    private sealed record Entries(ApiResponse Active, ApiResponse Draft);
}
