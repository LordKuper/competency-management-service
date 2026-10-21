using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The life of a matrix version: the hand-over check, the approval route of the superior step and the HR step with its skips, the returns
/// with a comment or with the approver's changes and the three decisions of the author, publication with the archiving of the previous version,
/// and the new version copied from the published one.
/// </summary>
public sealed class MatrixWorkflowTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string NotAllowedMessage = "Действие недоступно в текущем статусе версии";
    private const string TurnMessage = "Сейчас ход другого участника согласования";
    private const string ChangedByApproverMessage = "Версия изменена согласующим — верните её с правками";

    [Fact]
    public async Task Ac16_Route_GoesFromTheAuthorThroughTheSuperiorToTheHr_AndTheHrApprovalPublishes()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);

        var submitted = (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var atSuperior = (await kit.GetVersionAsync(org.DepartmentHead.Client)).Expect(HttpStatusCode.OK);
        var superiorApproved = (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        var atHr = (await kit.GetVersionAsync(org.Hr.Client)).Expect(HttpStatusCode.OK);
        var published = (await kit.TransitAsync(org.Hr.Client, "approve")).Expect(HttpStatusCode.OK);

        submitted.Json!["status"]!.GetValue<string>().Should().Be("OnReview");
        submitted.Json["step"]!.GetValue<string>().Should().Be("Superior");
        submitted.Json["superiorApprover"]!["userId"]!.GetValue<Guid>().Should().Be(org.DepartmentHead.Id);
        submitted.Json["hr"]!["userId"]!.GetValue<Guid>().Should().Be(org.Hr.Id);
        MatrixKit.Actions(submitted).Should().BeEmpty("it is the superior's turn, not the author's");
        MatrixKit.Actions(atSuperior).Should().BeEquivalentTo(["Edit", "Approve", "Return"]);
        superiorApproved.Json!["step"]!.GetValue<string>().Should().Be("Hr");
        superiorApproved.Json["status"]!.GetValue<string>().Should().Be("OnReview");
        MatrixKit.Actions(atHr).Should().BeEquivalentTo(["Edit", "Approve", "Return"]);
        published.Json!["status"]!.GetValue<string>().Should().Be("Published");
        published.Json["step"].Should().BeNull();
        published.Json["publishedAt"].Should().NotBeNull();
        MatrixKit.Actions(published).Should().BeEmpty();
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Approved@Superior", "Approved@Hr", "Published");
    }

    [Fact]
    public async Task Ac16_EveryTransition_IsJournaledWithoutTheDocument()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);

        var submitted = (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var returned = (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "Доработайте раздел" })).Expect(HttpStatusCode.OK);
        var edited = (await kit.SaveAsync(org.TeamHead.Client, kit.Content(4), await kit.ETagAsync())).Status;

        var submittedRows = await org.Admin.AuditOfRequestAsync(submitted.RequestId!);
        var returnedRows = await org.Admin.AuditOfRequestAsync(returned.RequestId!);
        submittedRows.Actions().Should().Contain("MatrixVersion.Submitted").And.Contain("MatrixVersion.Updated");
        returnedRows.Select(row => row!["action"]!.GetValue<string>()).Should().Contain("MatrixVersion.Returned");
        returnedRows.Single(row => row!["action"]!.GetValue<string>() == "MatrixVersion.Returned")!["reason"]!.GetValue<string>().Should().Be("Доработайте раздел");
        foreach (var row in submittedRows.Concat(returnedRows))
        {
            row!.ToJsonString().Should().NotContain("requirements", "the document is not written to the journal");
        }

        edited.Should().Be(HttpStatusCode.Conflict, "the head cannot edit a returned version, only decide on it");
    }

    [Fact]
    public async Task Ac15_HandOver_RefusesAnIncompleteVersion_NamingEveryProblem()
    {
        var org = await fixture.OrgAsync();
        var empty = await MatrixKit.CreateAsync(org);
        var badWeights = await MatrixKit.CreateAsync(org);
        var inactive = await MatrixKit.CreateAsync(org);

        var weights = badWeights.Content();
        weights["blockWeights"]![0]!["percent"] = 99.5m;
        weights["blockWeights"]!.AsArray().RemoveAt(1);
        (await badWeights.SaveAsync(org.TeamHead.Client, weights)).Expect(HttpStatusCode.OK);
        await inactive.PrepareAsync();
        (await org.Admin.PostAsync($"/api/v1/career/competencies/{inactive.Profile.Id}/deactivate", ifMatch: inactive.Profile.ETag)).Expect(HttpStatusCode.OK);

        var cases = new (MatrixKit Kit, string[] Problems)[]
        {
            (empty, ["В версии нет уровней"]),
            (badWeights, [
                $"Коэффициенты блоков уровня «{badWeights.Junior.Json!["name"]!.GetValue<string>()}» дают в сумме 99,5 %, нужно 100 %",
                $"Коэффициенты блоков уровня «{badWeights.Senior.Json!["name"]!.GetValue<string>()}» дают в сумме 0 %, нужно 100 %"]),
            (inactive, [$"В версии есть неактивная запись «{inactive.Profile.Json!["name"]!.GetValue<string>()}»"]),
        };

        foreach (var (kit, problems) in cases)
        {
            var refused = await kit.TransitAsync(org.TeamHead.Client, "submit");

            refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
            refused.Json!["errors"]!["content"]!.AsArray().Select(problem => problem!.GetValue<string>()).Should().Equal(problems);
            refused.Json["detail"]!.GetValue<string>().Should().Be(problems[0]);
            (await kit.StatusAsync()).Should().Be("Draft");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ac15_Approval_RefusesAVersionTheCatalogueMadeIncompleteAfterItWasHandedOn_AtBothSteps(bool atHrStep)
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        if (atHrStep)
        {
            (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        }

        (await org.Admin.PostAsync($"/api/v1/career/competencies/{kit.Profile.Id}/deactivate", ifMatch: kit.Profile.ETag)).Expect(HttpStatusCode.OK);

        var refused = await kit.TransitAsync(atHrStep ? org.Hr.Client : org.DepartmentHead.Client, "approve");

        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        refused.FieldErrors("content").Should().Equal([$"В версии есть неактивная запись «{kit.Profile.Json!["name"]!.GetValue<string>()}»"]);
        (await kit.StatusAsync()).Should().Be("OnReview");
        (await kit.DecisionsAsync()).Should().Equal(atHrStep ? ["Submitted", "Approved@Superior"] : ["Submitted"]);
    }

    [Fact]
    public async Task Ac16_Submit_IsRefusedWithoutAResponsibleHr_AndTheVersionStaysADraft()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        (await org.Admin.DeleteAsync($"/api/v1/career/org-units/{org.Root.Id}/hr")).Expect(HttpStatusCode.NoContent);

        var refused = await kit.TransitAsync(org.TeamHead.Client, "submit");

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Be("Нет ответственного HR — обратитесь к администратору");
        (await kit.StatusAsync()).Should().Be("Draft");
    }

    [Fact]
    public async Task Ac16_AnHrWhoCannotAct_IsNoResponsibleHr()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        var hr = await org.Admin.GetUserAsync(org.Hr.Id);
        (await org.Admin.PostAsync($"/api/v1/users/{org.Hr.Id}/block", ifMatch: hr.ETag)).Expect(HttpStatusCode.OK);

        var refused = await kit.TransitAsync(org.TeamHead.Client, "submit");

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Be("Нет ответственного HR — обратитесь к администратору");
    }

    [Fact]
    public async Task Ac16_TheSuperiorStep_IsSkippedWhenNoUnitAboveHasAHeadWhoCanAct_ToTheHr()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        await BlockAsync(org, org.DepartmentHead, org.RootHead);

        var submitted = (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        submitted.Json!["status"]!.GetValue<string>().Should().Be("OnReview");
        submitted.Json["step"]!.GetValue<string>().Should().Be("Hr");
        submitted.Json["superiorApprover"].Should().BeNull();
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Skipped@Superior");
        (await kit.TransitAsync(org.Hr.Client, "approve")).Expect(HttpStatusCode.OK).Json!["status"]!.GetValue<string>().Should().Be("Published");
    }

    [Fact]
    public async Task Ac16_TheSuperiorStep_GoesToTheNearestUnitAboveWhoseHeadCanAct()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        await BlockAsync(org, org.DepartmentHead);

        var submitted = (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        submitted.Json!["step"]!.GetValue<string>().Should().Be("Superior");
        submitted.Json["superiorApprover"]!["userId"]!.GetValue<Guid>().Should().Be(org.RootHead.Id);
        (await kit.TransitAsync(org.RootHead.Client, "approve")).Expect(HttpStatusCode.OK).Json!["step"]!.GetValue<string>().Should().Be("Hr");
    }

    [Fact]
    public async Task Ac16_WhenTheResponsibleHrIsTheAuthor_TheHrStepCountsAsApprovedAtOnce()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        var teamHead = await org.Admin.GrantHrRoleAsync(fixture.Host, org.TeamHead);
        (await org.Admin.AssignHrAsync(org.Team.Id, teamHead.EmployeeId)).Expect(HttpStatusCode.NoContent);

        var submitted = (await kit.TransitAsync(teamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var approved = (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);

        submitted.Json!["step"]!.GetValue<string>().Should().Be("Superior", "the superior still decides");
        approved.Json!["status"]!.GetValue<string>().Should().Be("Published");
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Approved@Superior", "AutoApproved@Hr", "Published");
    }

    [Fact]
    public async Task Ac16_WhenTheAuthorIsTheResponsibleHrAndNoSuperiorExists_SubmittingPublishesAtOnce()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        await BlockAsync(org, org.DepartmentHead, org.RootHead);
        var teamHead = await org.Admin.GrantHrRoleAsync(fixture.Host, org.TeamHead);
        (await org.Admin.AssignHrAsync(org.Team.Id, teamHead.EmployeeId)).Expect(HttpStatusCode.NoContent);

        var submitted = (await kit.TransitAsync(teamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        submitted.Json!["status"]!.GetValue<string>().Should().Be("Published");
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Skipped@Superior", "AutoApproved@Hr", "Published");
    }

    [Fact]
    public async Task Ac16_WhenTheHrIsAlsoTheSuperiorApprover_TheyApproveBothStepsSeparately()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        var departmentHead = await org.Admin.GrantHrRoleAsync(fixture.Host, org.DepartmentHead);
        (await org.Admin.AssignHrAsync(org.Team.Id, departmentHead.EmployeeId)).Expect(HttpStatusCode.NoContent);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        var first = (await kit.TransitAsync(departmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        var second = (await kit.TransitAsync(departmentHead.Client, "approve")).Expect(HttpStatusCode.OK);

        first.Json!["step"]!.GetValue<string>().Should().Be("Hr");
        second.Json!["status"]!.GetValue<string>().Should().Be("Published");
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Approved@Superior", "Approved@Hr", "Published");
    }

    [Fact]
    public async Task Ac16_ReturnWithAComment_SendsTheVersionBackWithoutChanges_AndTheAuthorMayOnlyReviseOrRollBack()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        var returned = (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "  Не хватает примеров  " })).Expect(HttpStatusCode.OK);
        var forAuthor = (await kit.GetVersionAsync(org.TeamHead.Client)).Expect(HttpStatusCode.OK);
        var acceptWithoutChanges = await kit.TransitAsync(org.TeamHead.Client, "accept-changes");
        var rolledBack = (await kit.TransitAsync(org.TeamHead.Client, "rollback-changes")).Expect(HttpStatusCode.OK);

        returned.Json!["status"]!.GetValue<string>().Should().Be("Returned");
        returned.Json["step"]!.GetValue<string>().Should().Be("Superior");
        forAuthor.Json!["returnedWithChanges"]!.GetValue<bool>().Should().BeFalse();
        MatrixKit.Actions(forAuthor).Should().BeEquivalentTo(["AcceptAndRevise", "RollbackChanges"]);
        acceptWithoutChanges.Status.Should().Be(HttpStatusCode.Conflict, "only a return with changes can be accepted as it is");
        acceptWithoutChanges.Json!["detail"]!.GetValue<string>().Should().Be(NotAllowedMessage);
        rolledBack.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        rolledBack.Json["step"].Should().BeNull();
        var comments = (await kit.HistoryAsync(org.Admin)).Json!["decisions"]!.AsArray().Select(decision => decision!["comment"]?.GetValue<string>()).ToList();
        comments.Should().Contain("Не хватает примеров");
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Returned@Superior", "RolledBack@Superior");
    }

    [Fact]
    public async Task Ac16_AcceptAndRevise_MakesTheReturnedVersionADraftAgain_KeepingTheApproversChanges()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.SaveAsync(org.DepartmentHead.Client, kit.Content(4))).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = (string?)null })).Expect(HttpStatusCode.OK);

        var revising = await kit.TransitAsync(org.TeamHead.Client, "accept-and-revise");

        revising.Status.Should().Be(HttpStatusCode.OK, revising.Body);
        revising.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        revising.Json["step"].Should().BeNull();
        revising.Json["content"]!["requirements"]![2]!["requiredScore"]!.GetValue<int>().Should().Be(4);
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Returned@Superior", "AcceptedAndRevising@Superior");
        MatrixKit.Actions(revising).Should().BeEquivalentTo(["Edit", "Submit"]);
    }

    [Fact]
    public async Task Ac16_ResubmittingAfterAReturn_StartsTheRouteFromItsFirstStepAgain()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "return", new { comment = "Ещё раз" })).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.TeamHead.Client, "rollback-changes")).Expect(HttpStatusCode.OK);

        var again = (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        again.Json!["status"]!.GetValue<string>().Should().Be("OnReview");
        again.Json["step"]!.GetValue<string>().Should().Be("Superior");
        (await kit.GetVersionAsync(org.DepartmentHead.Client)).Json!["actions"]!.AsArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Ac16_ApproverChanges_AreReturnedAsAVariantOfTheirOwn_AndApprovingUnreturnedChangesIsRefused()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.SaveAsync(org.DepartmentHead.Client, kit.Content(4))).Expect(HttpStatusCode.OK);

        var approval = await kit.TransitAsync(org.DepartmentHead.Client, "approve");
        var returned = (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = (string?)null })).Expect(HttpStatusCode.OK);
        var forAuthor = (await kit.GetVersionAsync(org.TeamHead.Client)).Expect(HttpStatusCode.OK);
        var history = (await kit.HistoryAsync(org.TeamHead.Client)).Expect(HttpStatusCode.OK).Json!;
        var diff = (await org.TeamHead.Client.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/variants/2/diff")).Expect(HttpStatusCode.OK).Json!;

        approval.Status.Should().Be(HttpStatusCode.Conflict);
        approval.Json!["detail"]!.GetValue<string>().Should().Be(ChangedByApproverMessage);
        returned.Json!["status"]!.GetValue<string>().Should().Be("Returned");
        forAuthor.Json!["returnedWithChanges"]!.GetValue<bool>().Should().BeTrue();
        MatrixKit.Actions(forAuthor).Should().BeEquivalentTo(["AcceptChanges", "AcceptAndRevise", "RollbackChanges"]);
        history["variants"]!.AsArray().Select(variant => variant!["kind"]!.GetValue<string>()).Should().Equal("Author", "Approver");
        diff["number"]!.GetValue<int>().Should().Be(2);
        diff["previousNumber"]!.GetValue<int>().Should().Be(1);
        diff["changes"]!.AsArray().Select(change => $"{change!["kind"]!.GetValue<string>()}/{change["type"]!.GetValue<string>()}")
            .Should().Equal("Requirement/Changed");
        diff["before"]!["requirements"]![2]!["requiredScore"]!.GetValue<int>().Should().Be(3);
        diff["after"]!["requirements"]![2]!["requiredScore"]!.GetValue<int>().Should().Be(4);
    }

    [Fact]
    public async Task Ac16_AcceptingTheApproversChanges_CountsAsApprovingTheReturningStep_AndTheRouteGoesOn()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.SaveAsync(org.DepartmentHead.Client, kit.Content(4))).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = (string?)null })).Expect(HttpStatusCode.OK);

        var accepted = (await kit.TransitAsync(org.TeamHead.Client, "accept-changes")).Expect(HttpStatusCode.OK);
        var published = (await kit.TransitAsync(org.Hr.Client, "approve")).Expect(HttpStatusCode.OK);

        accepted.Json!["status"]!.GetValue<string>().Should().Be("OnReview");
        accepted.Json["step"]!.GetValue<string>().Should().Be("Hr");
        published.Json!["content"]!["requirements"]![2]!["requiredScore"]!.GetValue<int>().Should().Be(4);
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Returned@Superior", "AcceptedChanges@Superior", "Approved@Hr", "Published");
    }

    [Fact]
    public async Task Ac16_AcceptingTheHrsChanges_PublishesTheVersionAtOnce()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        (await kit.SaveAsync(org.Hr.Client, kit.Content(4))).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "return", new { comment = "Поправил" })).Expect(HttpStatusCode.OK);

        var accepted = (await kit.TransitAsync(org.TeamHead.Client, "accept-changes")).Expect(HttpStatusCode.OK);

        accepted.Json!["status"]!.GetValue<string>().Should().Be("Published");
        accepted.Json["content"]!["requirements"]![2]!["requiredScore"]!.GetValue<int>().Should().Be(4);
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Approved@Superior", "Returned@Hr", "AcceptedChanges@Hr", "Published");
    }

    [Fact]
    public async Task Ac16_AnHrReturnWithOnlyAComment_AfterTheSuperiorsAcceptedChanges_CannotBeAcceptedAsItIs()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.SaveAsync(org.DepartmentHead.Client, kit.Content(4))).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = (string?)null })).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.TeamHead.Client, "accept-changes")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "return", new { comment = "Нужны примеры" })).Expect(HttpStatusCode.OK);

        var forAuthor = (await kit.GetVersionAsync(org.TeamHead.Client)).Expect(HttpStatusCode.OK);
        var accepted = await kit.TransitAsync(org.TeamHead.Client, "accept-changes");

        forAuthor.Json!["status"]!.GetValue<string>().Should().Be("Returned");
        forAuthor.Json["returnedWithChanges"]!.GetValue<bool>().Should().BeFalse("the HR changed nothing; the changes in the document are the superior's, already accepted");
        MatrixKit.Actions(forAuthor).Should().BeEquivalentTo(["AcceptAndRevise", "RollbackChanges"]);
        accepted.Status.Should().Be(HttpStatusCode.Conflict, accepted.Body);
        accepted.Json!["detail"]!.GetValue<string>().Should().Be(NotAllowedMessage);
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Returned@Superior", "AcceptedChanges@Superior", "Returned@Hr");
    }

    [Fact]
    public async Task Ac16_RollingBackTheChanges_ReturnsTheAuthorsVariantAsADraft()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.SaveAsync(org.DepartmentHead.Client, kit.Content(4))).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = (string?)null })).Expect(HttpStatusCode.OK);

        var back = (await kit.TransitAsync(org.TeamHead.Client, "rollback-changes")).Expect(HttpStatusCode.OK);

        back.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        back.Json["content"]!["requirements"]![2]!["requiredScore"]!.GetValue<int>().Should().Be(3);
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Returned@Superior", "RolledBack@Superior");
    }

    [Fact]
    public async Task Ac16_Return_NeedsACommentOrChanges_AndKeepsTheCommentWithinTheLimit()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        var nothing = await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "   " });
        var tooLong = await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = new string('я', 4001) });
        var longest = await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = new string('я', 4000) });

        nothing.Status.Should().Be(HttpStatusCode.BadRequest);
        nothing.FieldErrors("comment").Should().Equal(["Укажите комментарий или внесите правки"]);
        tooLong.Status.Should().Be(HttpStatusCode.BadRequest);
        tooLong.FieldErrors("comment").Should().Equal(["Не длиннее 4000 символов"]);
        longest.Status.Should().Be(HttpStatusCode.OK, longest.Body);
    }

    [Fact]
    public async Task Ac15_Return_WithChangesThatLeaveTheVersionIncomplete_IsRefused()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var incomplete = kit.Content();
        incomplete["competencies"]!.AsArray().Clear();
        incomplete["requirements"]!.AsArray().Clear();
        (await kit.SaveAsync(org.DepartmentHead.Client, incomplete)).Expect(HttpStatusCode.OK);

        var refused = await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "Убрал" });

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["errors"]!["content"]!.AsArray().Select(problem => problem!.GetValue<string>())
            .Should().Equal(["В матрице нет компетенций"]);
        (await kit.StatusAsync()).Should().Be("OnReview");
    }

    [Fact]
    public async Task Ac15_Publication_ArchivesThePreviousPublishedVersion_AndANewVersionStartsAsACopyOfThePublishedOne()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        await kit.PublishAsync();
        var firstId = kit.VersionId;

        var second = (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created);
        var third = await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions");
        var byHr = await org.Hr.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions");
        var secondId = second.Json!["id"]!.GetValue<Guid>();
        (await kit.TransitAsync(org.TeamHead.Client, "submit", versionId: secondId)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve", versionId: secondId)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "approve", versionId: secondId)).Expect(HttpStatusCode.OK);

        second.Json["number"]!.GetValue<int>().Should().Be(2);
        second.Json["status"]!.GetValue<string>().Should().Be("Draft");
        foreach (var part in new[] { "levels", "competencies", "blockWeights", "requirements" })
        {
            second.Json["content"]![part]!.ToJsonString().Should().Be(kit.Content()[part]!.ToJsonString(), $"the new version copies the published {part}");
        }

        third.Status.Should().Be(HttpStatusCode.Conflict);
        third.Json!["detail"]!.GetValue<string>().Should().Be("У матрицы уже есть версия в работе");
        byHr.Status.Should().Be(HttpStatusCode.Forbidden);
        (await kit.StatusAsync(firstId)).Should().Be("Archived");
        (await kit.StatusAsync(secondId)).Should().Be("Published");
        var matrix = (await org.Plain.Client.GetAsync($"/api/v1/career/matrices/{kit.MatrixId}")).Expect(HttpStatusCode.OK).Json!;
        matrix["publishedVersion"]!["number"]!.GetValue<int>().Should().Be(2);
        matrix["inWorkVersion"].Should().BeNull();
        var versions = (await org.Plain.Client.GetAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.OK).Json!.AsArray();
        versions.Select(version => $"{version!["number"]!.GetValue<int>()}:{version["status"]!.GetValue<string>()}").Should().Equal("2:Published", "1:Archived");
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM matrix_versions WHERE matrix_id = '{kit.MatrixId}' AND status = 'Published'")).Should().Be(1);
        (await org.Admin.AuditOfRequestAsync(second.RequestId!)).Actions().Should().Contain("MatrixVersion.Created");
    }

    [Fact]
    public async Task Ac15_PublishedAndArchivedVersions_AreImmutable()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        await kit.PublishAsync();
        var published = kit.VersionId;
        var next = (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created).Json!["id"]!.GetValue<Guid>();
        (await kit.TransitAsync(org.TeamHead.Client, "submit", versionId: next)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve", versionId: next)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "approve", versionId: next)).Expect(HttpStatusCode.OK);

        foreach (var id in new[] { published, next })
        {
            var edit = await kit.SaveAsync(org.TeamHead.Client, kit.Content(4), versionId: id);
            var submit = await kit.TransitAsync(org.TeamHead.Client, "submit", versionId: id);
            var approve = await kit.TransitAsync(org.Hr.Client, "approve", versionId: id);

            foreach (var refused in new[] { edit, submit, approve })
            {
                refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
                refused.Json!["detail"]!.GetValue<string>().Should().Be(NotAllowedMessage);
            }
        }

        (await kit.GetVersionAsync(org.Admin, published)).Json!["content"]!["requirements"]![2]!["requiredScore"]!.GetValue<int>().Should().Be(3);
    }

    [Fact]
    public async Task Ac17_APublishedVersion_ReadsFromItsSnapshot_WhileANewVersionReadsTheCatalogueAsItIsNow()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        await kit.PublishAsync();
        var published = kit.VersionId;
        var oldName = kit.Junior.Json!["name"]!.GetValue<string>();
        var newName = Scenarios.Unique("Переименованный");
        (await org.Admin.PutAsync($"/api/v1/career/levels/{kit.Junior.Id}", new { name = newName }, (await org.Admin.GetAsync($"/api/v1/career/levels/{kit.Junior.Id}")).ETag)).Expect(HttpStatusCode.OK);
        (await org.Admin.PutAsync(
            $"/api/v1/career/tracks/{kit.Track.Id}",
            new { name = Scenarios.Unique("ТрекНовый") },
            (await org.Admin.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}")).ETag)).Expect(HttpStatusCode.OK);

        var frozen = (await kit.GetVersionAsync(org.Plain.Client, published)).Expect(HttpStatusCode.OK);
        var next = (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created);

        frozen.Json!["entries"]!["levels"]!.AsArray().Select(level => level!["name"]!.GetValue<string>()).Should().Contain(oldName).And.NotContain(newName);
        frozen.Json["entries"]!["track"]!["name"]!.GetValue<string>().Should().Be(kit.Track.Json!["name"]!.GetValue<string>());
        next.Json!["entries"]!["levels"]!.AsArray().Select(level => level!["name"]!.GetValue<string>()).Should().Contain(newName).And.NotContain(oldName);
    }

    [Fact]
    public async Task Ac9_Publication_MakesTheDraftsOfTheVersionActive_AndAddsTheirVersionToTheHistory()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var scale = (await org.TeamHead.Client.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Своя шкала"), points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);
        var kit = await (await MatrixKit.CreateAsync(org, scaleId: scale.Id, competencyAuthor: org.TeamHead.Client)).PrepareAsync();
        (await org.Plain.Client.GetAsync($"/api/v1/career/competencies/{kit.Critical.Id}")).Status.Should().Be(HttpStatusCode.NotFound, "it is still a draft nobody else reads");

        await kit.PublishAsync();

        (await org.Plain.Client.GetAsync($"/api/v1/career/scales/{scale.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Active");
        foreach (var competency in new[] { kit.Critical, kit.Profile })
        {
            (await org.Plain.Client.GetAsync($"/api/v1/career/competencies/{competency.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Active");
            var versions = (await org.Plain.Client.GetAsync($"/api/v1/career/competencies/{competency.Id}/versions")).Json!.AsArray();
            versions.Select(version => version!["status"]!.GetValue<string>()).Should().Equal("Active", "Draft");
        }
    }

    [Fact]
    public async Task Ac9_Publication_IsRefusedWhenADraftOfTheVersionWouldTakeANameAnActiveEntryHasNow()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var scaleName = Scenarios.Unique("Своя шкала");
        var scale = (await org.TeamHead.Client.PostAsync("/api/v1/career/scales", new { name = scaleName, points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);
        var kit = await (await MatrixKit.CreateAsync(org, scaleId: scale.Id, competencyAuthor: org.TeamHead.Client)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        (await org.Admin.MakeScaleAsync(scaleName.ToUpperInvariant())).Expect(HttpStatusCode.Created);

        var refused = await kit.TransitAsync(org.Hr.Client, "approve");

        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        refused.Json!["detail"]!.GetValue<string>().Should().Be("Версию нельзя опубликовать");
        refused.Json["blockers"]!.AsArray().Select(blocker => blocker!["name"]!.GetValue<string>()).Should().Contain(scaleName);
        (await kit.StatusAsync()).Should().Be("OnReview");
        (await org.Admin.GetAsync($"/api/v1/career/scales/{scale.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Draft");
    }

    [Fact]
    public async Task Ac10_Publication_MakesActiveTwoDraftCompetenciesOfOneName_WhenOneIsOfTheDirectionAndTheOtherGeneral()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org, competencyAuthor: org.TeamHead.Client);
        var body = new
        {
            name = kit.Critical.Json!["name"]!.GetValue<string>(),
            description = (string?)null,
            directionId = (Guid?)null,
            scaleId = Guid.Parse(CareerScenarios.BaseScaleId),
        };
        (await org.TeamHead.Client.PutAsync($"/api/v1/career/competencies/{kit.Profile.Id}", body, kit.Profile.ETag)).Expect(HttpStatusCode.OK);

        await (await kit.PrepareAsync()).PublishAsync();

        foreach (var competency in new[] { kit.Critical, kit.Profile })
        {
            (await org.Plain.Client.GetAsync($"/api/v1/career/competencies/{competency.Id}")).Json!["status"]!.GetValue<string>().Should().Be("Active", "the names are unique within a direction and among the general competencies, not across them");
        }
    }

    [Fact]
    public async Task Ac16_Actions_AreOpenOnlyToTheParticipantWhoseTurnItIs()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);

        var draftByApprover = await kit.TransitAsync(org.DepartmentHead.Client, "submit");
        var draftEditByApprover = await kit.SaveAsync(org.DepartmentHead.Client, kit.Content(4));
        var draftByAdmin = await kit.TransitAsync(org.Admin, "submit");
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var approveByHr = await kit.TransitAsync(org.Hr.Client, "approve");
        var approveByAuthor = await kit.TransitAsync(org.TeamHead.Client, "approve");
        var approveByAdmin = await kit.TransitAsync(org.Admin, "approve");
        var editByAuthor = await kit.SaveAsync(org.TeamHead.Client, kit.Content(4));
        var approveByStranger = await kit.TransitAsync(org.Plain.Client, "approve");
        var approveByOtherHr = await kit.TransitAsync(org.SiblingHr.Client, "approve");

        foreach (var refused in new[] { draftByApprover, draftEditByApprover, draftByAdmin, approveByHr, approveByAuthor, approveByAdmin, editByAuthor })
        {
            refused.Status.Should().Be(HttpStatusCode.Forbidden, refused.Body);
            refused.Json!["detail"]!.GetValue<string>().Should().Be(TurnMessage);
        }

        approveByStranger.Status.Should().Be(HttpStatusCode.NotFound, "a user who cannot read the version does not learn that it exists");
        approveByOtherHr.Status.Should().Be(HttpStatusCode.NotFound);
        (await kit.StatusAsync()).Should().Be("OnReview");
    }

    [Fact]
    public async Task Ac17_ATransition_WithAStaleVersion_IsRefusedWith412_AndChangesNothing()
    {
        var org = await fixture.OrgAsync();
        var kit = await ReadyAsync(org);
        var opened = await kit.ETagAsync();
        (await kit.SaveAsync(org.TeamHead.Client, kit.Content(4), opened)).Expect(HttpStatusCode.OK);

        var stale = await kit.TransitAsync(org.TeamHead.Client, "submit", etag: opened);

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        (await kit.StatusAsync()).Should().Be("Draft");
        (await kit.DecisionsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Ac17_AStaleVersion_IsRefusedWith412_BeforeTheHandOverCheckJudgesTheDocument()
    {
        var org = await fixture.OrgAsync();
        var kit = await MatrixKit.CreateAsync(org);
        var opened = await kit.ETagAsync();
        (await kit.SaveAsync(org.TeamHead.Client, kit.Content(4), opened)).Expect(HttpStatusCode.OK);

        var stale = await kit.TransitAsync(org.TeamHead.Client, "submit", etag: opened);

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed, stale.Body);
        (await kit.StatusAsync()).Should().Be("Draft");
    }

    [Fact]
    public async Task Ac16_TheRoute_IsWorkedOutAtTheMomentOfActing_SoANewHeadTakesTheStepOverAtOnce()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var employee = await org.Admin.CreateEmployeeAsync(org.Department.Id);
        var account = await org.Admin.CreateUserAsync(employeeId: employee.Id);
        var successor = await account.SignInAsync(fixture.Host);

        (await org.Admin.SetHeadAsync(org.Department, employee.Id)).Expect(HttpStatusCode.OK);

        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Status.Should().Be(HttpStatusCode.NotFound, "the former head no longer even reads the version");
        (await kit.GetVersionAsync(successor)).Expect(HttpStatusCode.OK).Json!["actions"]!.AsArray().Should().NotBeEmpty();
        (await kit.TransitAsync(successor, "approve")).Expect(HttpStatusCode.OK).Json!["step"]!.GetValue<string>().Should().Be("Hr");
    }

    [Fact]
    public async Task Ac16_AVersionWaitingAtTheSuperiorStep_PassesToTheHr_WhenNoSuperiorCanActAnyMore()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await ReadyAsync(org);
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        await BlockAsync(org, org.DepartmentHead, org.RootHead);

        var waiting = (await kit.GetVersionAsync(org.Hr.Client)).Expect(HttpStatusCode.OK);
        var approved = (await kit.TransitAsync(org.Hr.Client, "approve")).Expect(HttpStatusCode.OK);

        waiting.Json!["step"]!.GetValue<string>().Should().Be("Hr");
        MatrixKit.Actions(waiting).Should().Contain("Approve");
        approved.Json!["status"]!.GetValue<string>().Should().Be("Published");
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Skipped@Superior", "Approved@Hr", "Published");
    }

    [Fact]
    public async Task Ac13_AScaleOfAPublishedVersion_CannotBeChanged_WhileOneOfAVersionInWorkCan()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var published = await MatrixKit.CreateAsync(org);
        var scale = await org.Admin.MakeScaleAsync();
        await MatrixKit.CreateAsync(org, scaleId: scale.Id);
        await (await published.PrepareAsync()).PublishAsync();
        var baseScale = (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK);

        var changeUsedByPublished = await org.Admin.PutAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}", new { name = Scenarios.Unique("Базовая"), points = CareerScenarios.Points() }, baseScale.ETag);
        var changeUsedByInWork = await org.Admin.PutAsync($"/api/v1/career/scales/{scale.Id}", new { name = Scenarios.Unique("Своя"), points = CareerScenarios.Points(5) }, scale.ETag);

        changeUsedByPublished.Status.Should().Be(HttpStatusCode.Conflict);
        changeUsedByPublished.Json!["detail"]!.GetValue<string>().Should().Be("Используется в опубликованной матрице — изменить нельзя");
        changeUsedByInWork.Status.Should().Be(HttpStatusCode.OK, changeUsedByInWork.Body);
    }

    [Fact]
    public async Task Ac10_ANewVersionOfAPublishedMatrix_CanBeStartedAfterItsTrackOrDirectionWasDeactivated()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        (await org.Admin.PostAsync($"/api/v1/career/tracks/{kit.Track.Id}/deactivate", ifMatch: (await org.Admin.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}")).ETag)).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/career/directions/{kit.Direction.Id}/deactivate", ifMatch: kit.Direction.ETag)).Expect(HttpStatusCode.OK);

        var next = await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions");
        var another = await org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id, directionId = kit.Direction.Id, trackId = kit.Track.Id });

        next.Status.Should().Be(HttpStatusCode.Created, next.Body);
        another.Status.Should().Be(HttpStatusCode.BadRequest, "an inactive track or direction is closed to new matrices only");
        another.FieldErrors("directionId").Should().Equal(["Запись не найдена или недоступна"]);
    }

    private async Task<MatrixKit> ReadyAsync(CareerOrg org) => await (await MatrixKit.CreateAsync(org)).PrepareAsync();

    private static async Task BlockAsync(CareerOrg org, params Member[] members)
    {
        foreach (var member in members)
        {
            var account = await org.Admin.GetUserAsync(member.Id);
            (await org.Admin.PostAsync($"/api/v1/users/{member.Id}/block", ifMatch: account.ETag)).Expect(HttpStatusCode.OK);
        }
    }
}
