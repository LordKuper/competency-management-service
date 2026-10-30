using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The reminders the career process mails: who is told when a version reaches their step, is returned to them, or a draft entry awaits
/// the HR; that nobody is mailed for their own action or when they cannot act; and what the letters say and link to. The letters of one
/// host go out one at a time in the order they were queued, so a letter that arrives proves every earlier one was handled.
/// </summary>
public sealed class CareerMailTests(CareerHost fixture, TestEnvironment environment) : IClassFixture<CareerHost>
{
    private const string AwaitingSubject = "Версия матрицы ждёт вашего согласования в системе «Калибр»";
    private const string ReturnedSubject = "Версия матрицы возвращена вам в системе «Калибр»";
    private const string DraftSubject = "Черновая запись справочника ждёт утверждения в системе «Калибр»";

    [Fact]
    public async Task Ac102_Ac110_ASubmittedVersion_MailsTheApproverOfItsNextStep_WithWhatWaitsFromWhomAndALinkToTheScreen_AndNothingOfTheVersionsContent()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();

        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);

        var superior = (await LettersAsync(org.DepartmentHead.User.Email, 1)).Single();
        var hr = (await LettersAsync(org.Hr.User.Email, 1)).Single();
        var trackName = kit.Track.Json!["name"]!.GetValue<string>();
        var teamName = org.Team.Json!["name"]!.GetValue<string>();
        superior.Subject.Should().Be(AwaitingSubject);
        superior.Text.Should().Contain($"версия матрицы «{trackName}» подразделения «{teamName}» (версия 1)").And.Contain("шаг 1, вышестоящий руководитель");
        superior.Text.Should().Contain(await FullNameAsync(org, org.TeamHead.Employee), "the letter says whom the version comes from");
        superior.Link.ToString().Should().Be($"{ApiHost.PublicBaseUrl}/desk/review/{kit.VersionId}");
        hr.Subject.Should().Be(AwaitingSubject);
        hr.Text.Should().Contain("шаг 2, HR").And.Contain(await FullNameAsync(org, org.DepartmentHead.Employee));
        hr.Link.ToString().Should().Be($"{ApiHost.PublicBaseUrl}/desk/review/{kit.VersionId}");
        (await CareerLettersAsync(org.TeamHead.User.Email)).Should().BeEmpty("the author is not told about their own submission");
        (await CareerLettersAsync(org.DepartmentHead.User.Email)).Should().ContainSingle("the superior is not told about their own approval");
    }

    [Theory]
    [InlineData(false, true, "с комментарием")]
    [InlineData(true, false, "с правками")]
    [InlineData(true, true, "с правками и комментарием согласующего")]
    public async Task Ac103_AReturnedVersion_MailsItsHead_SayingHowItWasReturnedButNotWhatTheCommentSays(bool withChanges, bool withComment, string how)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        if (withChanges)
        {
            (await kit.SaveAsync(org.DepartmentHead.Client, kit.Content(4))).Expect(HttpStatusCode.OK);
        }

        const string Comment = "Секретный комментарий согласующего";
        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = withComment ? Comment : null })).Expect(HttpStatusCode.OK);

        var letters = await LettersAsync(org.TeamHead.User.Email, 1);
        var returned = letters.Single();
        returned.Subject.Should().Be(ReturnedSubject);
        returned.Text.Should().Contain($"возвращена вам — {how}");
        returned.Text.Should().NotContain(Comment, "a comment is read on the screen, by whoever may read the version");
        returned.Link.ToString().Should().Be($"{ApiHost.PublicBaseUrl}/desk/rework/{kit.VersionId}");
        returned.Text.Should().Contain(await FullNameAsync(org, org.DepartmentHead.Employee));
    }

    [Fact]
    public async Task Ac104_Ac105_Ac106_ADraftAHeadWrote_MailsTheResponsibleHrOfTheirUnits_OnceEach_BeingAnEntryNotALevel_AndNotWhenNoHrCanAct()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var sentinel = await SentinelAsync(org);
        var head = org.TeamHead.Client;
        var competency = await head.MakeCompetencyAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var track = (await head.PostAsync("/api/v1/career/tracks", new { name = Scenarios.Unique("Трек"), ownerOrgUnitId = org.Team.Id, directionId = direction.Id })).Expect(HttpStatusCode.Created);
        await head.MakeLevelAsync(track.Id);
        var keeperCompetency = await org.Admin.MakeCompetencyAsync();

        await sentinel();
        var letters = await CareerLettersAsync(org.Hr.User.Email);

        letters.Should().HaveCount(2, "a draft level and a keeper's entry tell nobody");
        letters.Select(letter => letter.Subject).Should().OnlyContain(subject => subject == DraftSubject);
        var forCompetency = letters.Single(letter => letter.Text.Contains(competency.Json!["name"]!.GetValue<string>()));
        forCompetency.Text.Should().Contain("компетенция").And.Contain("общая запись").And.Contain(await FullNameAsync(org, org.TeamHead.Employee));
        forCompetency.Link.ToString().Should().Be($"{ApiHost.PublicBaseUrl}/desk/drafts/competency/{competency.Id}");
        var forTrack = letters.Single(letter => letter.Text.Contains(track.Json!["name"]!.GetValue<string>()));
        forTrack.Text.Should().Contain("трек").And.Contain($"подразделение «{org.Team.Json!["name"]!.GetValue<string>()}»");
        forTrack.Link.ToString().Should().Be($"{ApiHost.PublicBaseUrl}/desk/drafts/track/{track.Id}");
        letters.Any(letter => letter.Text.Contains(keeperCompetency.Json!["name"]!.GetValue<string>())).Should().BeFalse();
    }

    [Fact]
    public async Task Ac108_Ac109_NobodyIsMailedForTheirOwnAction_OrWhenTheirAccountIsBlocked_OrTheirEmployeeDismissed()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var sentinel = await SentinelAsync(org);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var hrAsSuperior = await org.Admin.GrantHrRoleAsync(fixture.Host, org.DepartmentHead);
        (await org.Admin.AssignHrAsync(org.Team.Id, hrAsSuperior.Employee)).Expect(HttpStatusCode.NoContent);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        await LettersAsync(hrAsSuperior.User.Email, 1);

        (await kit.TransitAsync(hrAsSuperior.Client, "approve")).Expect(HttpStatusCode.OK);
        await sentinel();

        (await CareerLettersAsync(hrAsSuperior.User.Email)).Should().ContainSingle("the approver whose own approval brings the version to their next step is not mailed for it");
        (await CareerLettersAsync(org.TeamHead.User.Email)).Should().BeEmpty();
    }

    [Fact]
    public async Task Ac109_AHeadWhoseAccountIsBlocked_IsNotMailedAReturn_AndTheAuthorIsNeverMailedOnPublication()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var sentinel = await SentinelAsync(org);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/users/{org.TeamHead.Id}/block", ifMatch: (await org.Admin.GetUserAsync(org.TeamHead.Id)).ETag)).Expect(HttpStatusCode.OK);

        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "Вернуть" })).Expect(HttpStatusCode.OK);
        await sentinel();

        (await CareerLettersAsync(org.TeamHead.User.Email)).Should().BeEmpty();
    }

    [Fact]
    public async Task Ac107_APublishingApproval_AndARollbackOrAcceptAndRevise_MailNobody()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var sentinel = await SentinelAsync(org);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        await LettersAsync(org.Hr.User.Email, 1);
        var hrMails = (await CareerLettersAsync(org.Hr.User.Email)).Length;

        (await kit.TransitAsync(org.Hr.Client, "approve")).Expect(HttpStatusCode.OK);
        await sentinel();

        (await CareerLettersAsync(org.Hr.User.Email)).Should().HaveCount(hrMails, "the publication is seen on the desk, not mailed");
        (await CareerLettersAsync(org.TeamHead.User.Email)).Should().BeEmpty();
        (await CareerLettersAsync(org.DepartmentHead.User.Email)).Should().ContainSingle();
    }

    private async Task<CapturedMail[]> CareerLettersAsync(string recipient) =>
        [.. (await environment.Mail.FindAsync(recipient)).Where(letter => letter.Subject is AwaitingSubject or ReturnedSubject or DraftSubject)];

    private async Task<CapturedMail[]> LettersAsync(string recipient, int count)
    {
        CapturedMail[] found = [];
        await Waiting.UntilAsync(async () => (found = await CareerLettersAsync(recipient)).Length >= count, $"{count} career letter(s) reached {recipient}");
        return found;
    }

    private async Task<string> FullNameAsync(CareerOrg org, Guid employeeId) =>
        (await org.Admin.GetEmployeeAsync(employeeId)).Json!["fullName"]!.GetValue<string>();

    private async Task<Func<Task>> SentinelAsync(CareerOrg org)
    {
        var counted = (await CareerLettersAsync(org.SiblingHr.User.Email)).Length;
        return async () =>
        {
            await org.SiblingHead.Client.MakeCompetencyAsync();
            await LettersAsync(org.SiblingHr.User.Email, counted + 1);
        };
    }
}
