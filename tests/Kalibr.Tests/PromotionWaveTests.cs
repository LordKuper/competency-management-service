using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Announcing a career session and the settings behind it: who may choose which units, the commission with a chair every unit needs, one unfinished
/// session a unit, the dates the announcement accepts, what an announcement tells people, and the desk cards that lead to the commission and to the announcement.
/// </summary>
public sealed class PromotionWaveTests(CareerHost fixture, TestEnvironment environment) : IClassFixture<CareerHost>
{
    private const string Waves = "/api/v1/promotion/waves";
    private const string Settings = "/api/v1/promotion/settings";

    private static object Body(Guid[] unitIds, DateOnly? start = null, DateOnly[]? ends = null)
    {
        var first = start ?? PromotionScene.Today.AddDays(9);
        return new { startsOn = first, endsOn = ends ?? PromotionScene.Ends(first), unitIds };
    }

    private static async Task GiveChairAsync(CareerOrg org, Guid unitId, Member chair) =>
        (await PromotionScene.SaveCommissionAsync(org.Admin, unitId, [chair.Employee], chair.Employee)).Expect(HttpStatusCode.OK);

    [Fact]
    public async Task Ac66_Ac1_ASessionCannotBeAnnounced_ForAUnitWithoutAChair_OrOutsideTheHrsResponsibility()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var noChair = await org.SiblingHr.Client.PostAsync(Waves, Body([org.Sibling.Id]));
        await GiveChairAsync(org, org.Team.Id, org.TeamHead);
        var outside = await org.SiblingHr.Client.PostAsync(Waves, Body([org.Team.Id]));
        var nobody = await org.SiblingHr.Client.PostAsync(Waves, Body([]));
        var byUser = await org.Plain.Client.PostAsync(Waves, Body([org.Team.Id]));

        noChair.Status.Should().Be(HttpStatusCode.BadRequest);
        noChair.FieldErrors("unitIds").Should().ContainSingle().Which.Should().Contain("нет председателя");
        outside.Status.Should().Be(HttpStatusCode.BadRequest);
        outside.FieldErrors("unitIds").Should().ContainSingle().Which.Should().Contain("вне вашей ответственности");
        nobody.Status.Should().Be(HttpStatusCode.BadRequest);
        byUser.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ac2_Ac56_AUnitEntersOneUnfinishedSession_AndTheStartIsAWeekAfterTheAnnouncement()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        await GiveChairAsync(org, org.Sibling.Id, org.SiblingHead);
        var tooSoon = PromotionScene.Today.AddDays(3);

        var early = await org.SiblingHr.Client.PostAsync(Waves, Body([org.Sibling.Id], tooSoon));
        var announced = (await org.SiblingHr.Client.PostAsync(Waves, Body([org.Sibling.Id]))).Expect(HttpStatusCode.Created);
        var overlapping = await org.Admin.PostAsync(Waves, Body([org.Sibling.Id]));

        early.Status.Should().Be(HttpStatusCode.BadRequest);
        early.FieldErrors("startsOn").Should().NotBeEmpty();
        announced.Json!["units"]!.AsArray().Select(unit => unit!["id"]!.GetValue<Guid>()).Should().Equal(org.Sibling.Id);
        announced.Json["stage"]!.GetValue<int>().Should().Be(0);
        overlapping.Status.Should().Be(HttpStatusCode.BadRequest);
        overlapping.FieldErrors("unitIds").Should().ContainSingle().Which.Should().Contain("уже входит в незавершённую");
    }

    [Fact]
    public async Task Ac7_AnAnnouncedSession_IsSeenByEveryoneSignedIn_ButManagedAndListedOnlyByHrAndAdministrators()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        await GiveChairAsync(org, org.Team.Id, org.TeamHead);
        var announced = (await org.Hr.Client.PostAsync(Waves, Body([org.Team.Id]))).Expect(HttpStatusCode.Created);

        var open = (await org.Plain.Client.GetAsync($"{Waves}/open")).Expect(HttpStatusCode.OK).Json!.AsArray();
        var seen = open.Single(wave => wave!["id"]!.GetValue<Guid>() == announced.Id)!;
        var list = (await org.Hr.Client.GetAsync(Waves)).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();

        var unit = seen["units"]!.AsArray().Single()!;
        unit["id"]!.GetValue<Guid>().Should().Be(org.Team.Id);
        unit["targets"]!.AsArray().Select(target => target!["trackId"]!.GetValue<Guid>()).Should().Contain(kit.Track.Id);
        list.Select(wave => wave!["id"]!.GetValue<Guid>()).Should().Contain(announced.Id);
        (await org.Plain.Client.GetAsync(Waves)).Status.Should().Be(HttpStatusCode.Forbidden);
        (await org.Plain.Client.GetAsync($"{Waves}/{announced.Id}")).Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ac63_Ac87_TheEmployeesOfTheUnitsAreToldOfTheAnnouncement_ByALetterWithAClickableLink()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        await GiveChairAsync(org, org.Sibling.Id, org.SiblingHead);

        (await org.SiblingHr.Client.PostAsync(Waves, Body([org.Sibling.Id]))).Expect(HttpStatusCode.Created);

        CapturedMail[] letters = [];
        await Waiting.UntilAsync(
            async () => (letters = [.. (await environment.Mail.FindAsync(org.SiblingHead.User.Email)).Where(mail => mail.Text.Contains("карьерная сессия"))]).Length > 0,
            "the announcement reached the head");
        var letter = letters.Should().ContainSingle().Subject;
        letter.Html.Should().Contain($"<a href=\"{ApiHost.PublicBaseUrl}/promotion\">Посмотреть карьерную сессию</a>");
        letter.Text.Should().Contain("карьерная сессия");
    }

    [Fact]
    public async Task Ac66_ATheChairOfAUnitInAnUnfinishedSession_CannotBeRemoved()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        await GiveChairAsync(org, org.Sibling.Id, org.SiblingHead);
        (await org.SiblingHr.Client.PostAsync(Waves, Body([org.Sibling.Id]))).Expect(HttpStatusCode.Created);

        var removed = await PromotionScene.SaveCommissionAsync(org.SiblingHr.Client, org.Sibling.Id, [org.SiblingHead.Employee], null);
        var replaced = await PromotionScene.SaveCommissionAsync(org.SiblingHr.Client, org.Sibling.Id, [org.SiblingHead.Employee, org.TeamHead.Employee], org.TeamHead.Employee);

        removed.Status.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
        replaced.Expect(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac25_TheResponsibleHrAndTheHeadAreAlwaysInTheCommission_WhoeverIsSaved()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var route = $"/api/v1/promotion/commissions/{org.Team.Id}";

        var beforeSaving = (await org.Admin.GetAsync(route)).Expect(HttpStatusCode.OK).Json!["members"]!.AsArray();
        (await PromotionScene.SaveCommissionAsync(org.Admin, org.Team.Id, [org.DepartmentHead.Employee], org.DepartmentHead.Employee)).Expect(HttpStatusCode.OK);
        var withExtra = (await org.Admin.GetAsync(route)).Expect(HttpStatusCode.OK).Json!["members"]!.AsArray();
        (await PromotionScene.SaveCommissionAsync(org.Admin, org.Team.Id, [], null)).Expect(HttpStatusCode.OK);
        var emptied = (await org.Admin.GetAsync(route)).Expect(HttpStatusCode.OK).Json!["members"]!.AsArray();

        foreach (var members in new[] { beforeSaving, withExtra, emptied })
        {
            members.Where(member => member!["isRequired"]!.GetValue<bool>()).Select(member => member!["employeeId"]!.GetValue<Guid>())
                .Should().BeEquivalentTo([org.TeamHead.Employee, org.Hr.Employee]);
            members.Single(member => member!["employeeId"]!.GetValue<Guid>() == org.TeamHead.Employee)!["isHead"]!.GetValue<bool>().Should().BeTrue();
            members.Single(member => member!["employeeId"]!.GetValue<Guid>() == org.Hr.Employee)!["isResponsibleHr"]!.GetValue<bool>().Should().BeTrue();
        }

        withExtra.Select(member => member!["employeeId"]!.GetValue<Guid>()).Should().Contain(org.DepartmentHead.Employee);
        emptied.Select(member => member!["employeeId"]!.GetValue<Guid>()).Should().NotContain(org.DepartmentHead.Employee);
    }

    [Fact]
    public async Task Ac86_AUnitWhoseCommissionHasNoChair_IsAnActionOnTheDeskOfItsHrAndTheAdministrator_UntilAChairIsNamed()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var notFormed = await ReasonsAsync(org.SiblingHr, org.Sibling.Id);
        var byAdmin = (await org.Admin.ItemsAsync()).CommissionOf(org.Sibling.Id)?.Kinds();
        (await PromotionScene.SaveCommissionAsync(org.SiblingHr.Client, org.Sibling.Id, [], null)).Expect(HttpStatusCode.OK);
        var saved = await ReasonsAsync(org.SiblingHr, org.Sibling.Id);
        await GiveChairAsync(org, org.Sibling.Id, org.SiblingHead);
        var named = await ReasonsAsync(org.SiblingHr, org.Sibling.Id);

        notFormed.Should().Equal("CommissionNotFormed");
        byAdmin.Should().Equal("CommissionNotFormed");
        saved.Should().Equal("CommissionWithoutChair");
        named.Should().BeNull("the action goes as soon as a chair is named");
        (await ReasonsAsync(org.Plain, org.Sibling.Id)).Should().BeNull();
        (await ReasonsAsync(org.SiblingHead, org.Sibling.Id)).Should().BeNull("only the HR and the administrator are asked");
    }

    [Fact]
    public async Task Ac89_TheAnnouncementCardAppearsWhileAUnitWithAChairIsFreeOfSessions_AndGoesWhenASessionTakesIt()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var withoutChair = await PrerequisitesAsync(org.SiblingHr);
        await GiveChairAsync(org, org.Sibling.Id, org.SiblingHead);
        var withChair = await PrerequisitesAsync(org.SiblingHr);
        var forOthers = await PrerequisitesAsync(org.SiblingHead);
        (await org.SiblingHr.Client.PostAsync(Waves, Body([org.Sibling.Id]))).Expect(HttpStatusCode.Created);
        var announced = await PrerequisitesAsync(org.SiblingHr);

        withoutChair.Should().NotContain("WaveUnitAvailable");
        withChair.Should().Contain("WaveUnitAvailable");
        forOthers.Should().NotContain("WaveUnitAvailable");
        announced.Should().NotContain("WaveUnitAvailable", "the only unit of the HR is in a session");
    }

    [Fact]
    public async Task Ac71_TheSettingsStartWithTheDefaults_AreChangedByTheAdministratorAlone_AndAreChecked()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);

        var initial = (await org.Hr.Client.GetAsync(Settings)).Expect(HttpStatusCode.OK);
        var byHr = await org.Hr.Client.PutAsync(Settings, new { minIntervalMonths = 6, stageDurationsDays = new[] { 1, 1, 1, 1, 1, 1 } }, initial.ETag);
        var invalid = await org.Admin.PutAsync(Settings, new { minIntervalMonths = 6, stageDurationsDays = new[] { 1, 0, 1, 1, 1, 1 } }, initial.ETag);
        var changed = (await org.Admin.PutAsync(Settings, new { minIntervalMonths = 6, stageDurationsDays = new[] { 7, 1, 1, 1, 1, 1 } }, initial.ETag)).Expect(HttpStatusCode.OK);

        initial.Json!["minIntervalMonths"]!.GetValue<int>().Should().Be(12);
        initial.Json["stageDurationsDays"]!.AsArray().Select(days => days!.GetValue<int>()).Should().Equal(10, 2, 5, 2, 5, 5);
        byHr.Status.Should().Be(HttpStatusCode.Forbidden);
        invalid.Status.Should().Be(HttpStatusCode.BadRequest);
        invalid.FieldErrors("stageDurationsDays[1]").Should().NotBeEmpty();
        changed.Json!["minIntervalMonths"]!.GetValue<int>().Should().Be(6);
        (await org.Hr.Client.GetAsync(Settings)).Json!["stageDurationsDays"]![0]!.GetValue<int>().Should().Be(7);
    }

    private static async Task<string[]?> ReasonsAsync(Member member, Guid unitId) =>
        (await member.Client.ItemsAsync()).CommissionOf(unitId)?.Kinds();

    private static async Task<string[]> PrerequisitesAsync(Member member) =>
        [.. (await member.Client.DeskAsync())["prerequisites"]!.AsArray().Select(prerequisite => prerequisite!.GetValue<string>())];
}
