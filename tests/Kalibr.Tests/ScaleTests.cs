using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Scales: a scale has at least two distinct scores with their meanings, exactly one active scale is the default and a new competency gets it,
/// and making another scale the default moves the flag. These tests share one default flag, so they run on a host of their own, one after another.
/// </summary>
public sealed class ScaleTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string InactiveDefaultMessage = "По умолчанию может быть только действующая шкала";
    private const string DefaultDeactivationMessage = "Шкалу по умолчанию нельзя деактивировать — сначала сделайте по умолчанию другую шкалу";

    [Fact]
    public async Task Ac13_Points_AreCheckedWhenAScaleIsCreatedOrChanged()
    {
        var org = await fixture.OrgAsync();
        var scale = await org.Admin.MakeScaleAsync();
        var cases = new (object[] Points, string Message)[]
        {
            ([new { score = 0, label = "Один" }], "Укажите не меньше 2 баллов"),
            ([new { score = 0, label = "Нуль" }, new { score = 101, label = "Много" }], "Балл — целое число от 0 до 100"),
            ([new { score = -1, label = "Минус" }, new { score = 1, label = "Один" }], "Балл — целое число от 0 до 100"),
            ([new { score = 1, label = "Раз" }, new { score = 1, label = "Ещё раз" }], "Баллы не должны повторяться"),
            ([new { score = 0, label = "Нуль" }, new { score = 1, label = "  " }], "Укажите значение каждого балла"),
            ([new { score = 0, label = "Нуль" }, new { score = 1, label = new string('я', 501) }], "Значение балла — не длиннее 500 символов"),
        };

        foreach (var (points, message) in cases)
        {
            var created = await org.Admin.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Шкала"), points });
            var changed = await org.Admin.PutAsync($"/api/v1/career/scales/{scale.Id}", new { name = Scenarios.Unique("Шкала"), points }, scale.ETag);

            created.Status.Should().Be(HttpStatusCode.BadRequest, message);
            created.FieldErrors("points").Should().Equal([message]);
            changed.FieldErrors("points").Should().Equal([message]);
        }

        var longest = await org.Admin.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Шкала"), points = new object[] { new { score = 0, label = new string('я', 500) }, new { score = 100, label = "Верх" } } });
        longest.Status.Should().Be(HttpStatusCode.Created, longest.Body);
    }

    [Fact]
    public async Task Ac13_Points_AreStoredTrimmedInAscendingOrderOfScore()
    {
        var org = await fixture.OrgAsync();

        var created = (await org.Admin.PostAsync(
            "/api/v1/career/scales",
            new { name = Scenarios.Unique("Шкала"), points = new object[] { new { score = 3, label = " Три " }, new { score = 0, label = "Ноль" }, new { score = 1, label = "Один" } } })).Expect(HttpStatusCode.Created);

        created.Json!["points"]!.AsArray().Select(point => point!["score"]!.GetValue<int>()).Should().Equal(0, 1, 3);
        created.Json["points"]![2]!["label"]!.GetValue<string>().Should().Be("Три");
        created.Json["isDefault"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task Ac22_TheBaseScale_IsTheDefaultAndTheOnlyOne_ReadableByEveryone()
    {
        var org = await fixture.OrgAsync();
        await org.Admin.MakeScaleAsync();

        var defaults = (await org.Plain.Client.GetAsync("/api/v1/career/scales?isDefault=true")).Expect(HttpStatusCode.OK);
        var others = (await org.Plain.Client.GetAsync("/api/v1/career/scales?isDefault=false&pageSize=200")).Expect(HttpStatusCode.OK);

        defaults.ItemIds().Should().Equal(CareerScenarios.BaseScaleId);
        defaults.Json!["items"]![0]!["isDefault"]!.GetValue<bool>().Should().BeTrue();
        others.ItemIds().Should().NotContain(CareerScenarios.BaseScaleId);
        (await fixture.Host.ScalarAsync<long>("SELECT count(*) FROM competency_scales WHERE is_default")).Should().Be(1);
    }

    [Fact]
    public async Task Ac22_MakeDefault_MovesTheFlagToTheChosenActiveScale_ByAdministratorsAndAnyHr()
    {
        var org = await fixture.OrgAsync();
        var chosen = await org.Admin.MakeScaleAsync();
        var other = await org.Admin.MakeScaleAsync();

        var byHr = (await org.SiblingHr.Client.PostAsync($"/api/v1/career/scales/{chosen.Id}/make-default", ifMatch: chosen.ETag)).Expect(HttpStatusCode.OK);
        var current = await DefaultIdAsync(org.Plain.Client);
        var byAdmin = (await org.Admin.PostAsync($"/api/v1/career/scales/{other.Id}/make-default", ifMatch: other.ETag)).Expect(HttpStatusCode.OK);
        var afterAdmin = await DefaultIdAsync(org.Plain.Client);
        (await org.Admin.PostAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}/make-default", ifMatch: (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).ETag)).Expect(HttpStatusCode.OK);

        byHr.Json!["isDefault"]!.GetValue<bool>().Should().BeTrue();
        current.Should().Be(chosen.Id.ToString());
        byAdmin.Json!["isDefault"]!.GetValue<bool>().Should().BeTrue();
        afterAdmin.Should().Be(other.Id.ToString());
        (await DefaultIdAsync(org.Plain.Client)).Should().Be(CareerScenarios.BaseScaleId, "the flag is back on the base scale");
        (await fixture.Host.ScalarAsync<long>("SELECT count(*) FROM competency_scales WHERE is_default")).Should().Be(1);
    }

    [Fact]
    public async Task Ac22_MakeDefault_IsRefusedToAHeadAndAPlainUser_AndForAScaleThatIsNotActive()
    {
        var org = await fixture.OrgAsync();
        var active = await org.Admin.MakeScaleAsync();
        var draft = (await org.TeamHead.Client.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Шкала"), points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);
        var inactive = await org.Admin.MakeScaleAsync();
        (await org.Admin.PostAsync($"/api/v1/career/scales/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);

        var byHead = await org.TeamHead.Client.PostAsync($"/api/v1/career/scales/{active.Id}/make-default", ifMatch: active.ETag);
        var byPlain = await org.Plain.Client.PostAsync($"/api/v1/career/scales/{active.Id}/make-default", ifMatch: active.ETag);
        var ofDraft = await org.Hr.Client.PostAsync($"/api/v1/career/scales/{draft.Id}/make-default", ifMatch: draft.ETag);
        var ofInactive = await org.Hr.Client.PostAsync($"/api/v1/career/scales/{inactive.Id}/make-default", ifMatch: inactive.ETag);
        var unknown = await org.Hr.Client.PostAsync($"/api/v1/career/scales/{Guid.NewGuid()}/make-default", ifMatch: active.ETag);
        var stale = await org.Hr.Client.PostAsync($"/api/v1/career/scales/{active.Id}/make-default", ifMatch: "\"999\"");

        byHead.Status.Should().Be(HttpStatusCode.Forbidden);
        byPlain.Status.Should().Be(HttpStatusCode.Forbidden);
        foreach (var refused in new[] { ofDraft, ofInactive })
        {
            refused.Status.Should().Be(HttpStatusCode.Conflict);
            refused.Json!["detail"]!.GetValue<string>().Should().Be(InactiveDefaultMessage);
        }

        unknown.Status.Should().Be(HttpStatusCode.NotFound);
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        (await DefaultIdAsync(org.Plain.Client)).Should().Be(CareerScenarios.BaseScaleId);
    }

    [Fact]
    public async Task Ac22_TheDefaultScale_CannotBeDeactivated_UntilAnotherScaleIsTheDefault()
    {
        var org = await fixture.OrgAsync();
        var current = (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK);

        var refused = await org.Admin.PostAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}/deactivate", ifMatch: current.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Be(DefaultDeactivationMessage);
        (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Json!["status"]!.GetValue<string>().Should().Be("Active");
    }

    [Fact]
    public async Task Ac22_MakeDefault_Journals_TheScaleThatLosesTheFlagAndTheOneThatGetsIt()
    {
        var org = await fixture.OrgAsync();
        var chosen = await org.Admin.MakeScaleAsync();

        var made = (await org.Admin.PostAsync($"/api/v1/career/scales/{chosen.Id}/make-default", ifMatch: chosen.ETag)).Expect(HttpStatusCode.OK);
        var rows = await org.Admin.AuditOfRequestAsync(made.RequestId!);
        (await org.Admin.PostAsync(
            $"/api/v1/career/scales/{CareerScenarios.BaseScaleId}/make-default",
            ifMatch: (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).ETag)).Expect(HttpStatusCode.OK);

        rows.Select(row => row!["entityId"]!.GetValue<string>()).Should().BeEquivalentTo([CareerScenarios.BaseScaleId, chosen.Id.ToString()]);
        rows.Select(row => row!["action"]!.GetValue<string>()).Should().OnlyContain(action => action == "CompetencyScale.Updated");
        rows.Single(row => row!["entityId"]!.GetValue<string>() == chosen.Id.ToString())!["newValue"]!["isDefault"]!.GetValue<bool>().Should().BeTrue();
    }

    private static async Task<string> DefaultIdAsync(ApiClient client) =>
        (await client.GetAsync("/api/v1/career/scales?isDefault=true")).Expect(HttpStatusCode.OK).ItemIds().Single();
}
