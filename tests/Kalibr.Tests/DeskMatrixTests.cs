using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// What the desk says about a matrix version on its way: whose action it is at each step, who is only told for reference, when it
/// goes away, and that the desk agrees with the actions the server offers and refuses.
/// </summary>
public sealed class DeskMatrixTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string ApproveAction = "Approve";

    [Fact]
    public async Task Ac23_ADraftVersion_IsAnActionOfItsHeadAlone_WithTheBuilderStepItStoppedAt()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org);
        (await org.TeamHead.Client.PutAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/content?builderStep=3", kit.Content(), await kit.ETagAsync()))
            .Expect(HttpStatusCode.OK);

        var item = (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);

        item["group"]!.GetValue<string>().Should().Be("UnsubmittedVersions");
        item["isReference"]!.GetValue<bool>().Should().BeFalse();
        item["object"]!["type"]!.GetValue<string>().Should().Be("Matrix");
        item["target"]!["id"]!.GetValue<Guid>().Should().Be(kit.VersionId);
        item.Kinds().Should().Equal("VersionNotSubmitted");
        item.FirstReason()["occurredAt"].Should().NotBeNull("a draft is dated by its creation");
        item.FirstReason()["details"]!["step"]!.GetValue<int>().Should().Be(3);
        foreach (var other in new[] { org.DepartmentHead, org.RootHead, org.SiblingHead, org.Hr, org.Plain })
        {
            (await other.Client.ItemsAsync()).ItemOf(kit.MatrixId).Should().BeNull("a draft is the desk of its unit's head alone");
        }
    }

    [Fact]
    public async Task Ac24_Ac130_ASubmittedVersion_IsAnActionOfTheSuperiorHead_AndForReferenceOfItsOwnHead_AsTheServerOffersTheActions()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var countBefore = await org.DepartmentHead.Client.CountAsync();

        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        var superior = (await org.DepartmentHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);
        var author = (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);
        superior.Kinds().Should().Equal("VersionAwaitsHeadApproval");
        superior["group"]!.GetValue<string>().Should().Be("VersionsToApprove");
        superior["isReference"]!.GetValue<bool>().Should().BeFalse();
        superior.FirstReason()["occurredAt"].Should().NotBeNull("the wait is dated by the decision that brought the version to the step");
        superior.FirstReason()["details"]!["step"]!.GetValue<int>().Should().Be(1);
        superior.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.TeamHead.Employee);
        (await org.DepartmentHead.Client.CountAsync()).Should().Be(countBefore + 1);
        author.Kinds().Should().Equal("OwnVersionInApproval");
        author["group"]!.GetValue<string>().Should().Be("VersionsInApproval");
        author["isReference"]!.GetValue<bool>().Should().BeTrue();
        author.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.DepartmentHead.Employee);
        (await org.Hr.Client.ItemsAsync()).ItemOf(kit.MatrixId).Should().BeNull("the responsible HR is told only when the version waits at the superior step for long");
        MatrixKit.Actions((await kit.GetVersionAsync(org.DepartmentHead.Client)).Expect(HttpStatusCode.OK)).Should().Contain(ApproveAction);
        MatrixKit.Actions((await kit.GetVersionAsync(org.Hr.Client)).Expect(HttpStatusCode.OK)).Should().NotContain(ApproveAction);
        MatrixKit.Actions((await kit.GetVersionAsync(org.TeamHead.Client)).Expect(HttpStatusCode.OK)).Should().NotContain(ApproveAction);
    }

    [Fact]
    public async Task Ac27_Ac14_TheHrStep_MovesTheActionToTheResponsibleHr_AndThePublicationClearsEveryDesk()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);

        var hr = (await org.Hr.Client.ItemsAsync()).ItemFor(kit.MatrixId);
        hr.Kinds().Should().Equal("VersionAwaitsHrApproval");
        hr["isReference"]!.GetValue<bool>().Should().BeFalse();
        hr.FirstReason()["details"]!["step"]!.GetValue<int>().Should().Be(2);
        hr.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.TeamHead.Employee);
        (await org.DepartmentHead.Client.ItemsAsync()).ItemOf(kit.MatrixId).Should().BeNull("the superior's action is done");
        var author = (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);
        author.Kinds().Should().Equal("OwnVersionInApproval");
        author.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.Hr.Employee);
        author.FirstReason()["details"]!["step"]!.GetValue<int>().Should().Be(2);
        MatrixKit.Actions((await kit.GetVersionAsync(org.Hr.Client)).Expect(HttpStatusCode.OK)).Should().Contain(ApproveAction);

        (await kit.TransitAsync(org.Hr.Client, "approve")).Expect(HttpStatusCode.OK);

        foreach (var member in new[] { org.TeamHead, org.DepartmentHead, org.Hr })
        {
            (await member.Client.ItemsAsync()).ItemOf(kit.MatrixId).Should().BeNull("a published matrix needs nothing");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ac22_AReturnedVersion_IsAnActionOfItsAuthor_NamingWhatTheApproverReturnedItWith_AndBecomesADraftOnRevising(bool withChanges)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        if (withChanges)
        {
            (await kit.SaveAsync(org.DepartmentHead.Client, kit.Content(4))).Expect(HttpStatusCode.OK);
        }

        (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = withChanges ? null : "Не хватает примеров" })).Expect(HttpStatusCode.OK);

        var returned = (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId);
        returned.Kinds().Should().Equal("VersionReturned");
        returned["group"]!.GetValue<string>().Should().Be("ReturnedVersions");
        returned["isReference"]!.GetValue<bool>().Should().BeFalse();
        returned.FirstReason()["occurredAt"].Should().NotBeNull("a return is dated by the decision");
        returned.FirstReason()["details"]!["hasChanges"]!.GetValue<bool>().Should().Be(withChanges);
        returned.FirstReason()["details"]!["hasComment"]!.GetValue<bool>().Should().Be(!withChanges);
        returned.FirstReason()["details"]!["number"]!.GetValue<int>().Should().Be(1);
        (await org.DepartmentHead.Client.ItemsAsync()).ItemOf(kit.MatrixId).Should().BeNull("the approver's turn is over");

        (await kit.TransitAsync(org.TeamHead.Client, "accept-and-revise")).Expect(HttpStatusCode.OK);

        (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId).Kinds().Should().Equal("VersionNotSubmitted");
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(8, true)]
    public async Task Ac136_AVersionWaitingAtTheSuperiorStepForMoreThanSevenDays_IsToldToTheResponsibleHrForReference(int daysAgo, bool isShown)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        await fixture.Host.ExecuteAsync($"UPDATE matrix_version_decisions SET at = now() - interval '{daysAgo} days' WHERE version_id = '{kit.VersionId}'");

        var hr = (await org.Hr.Client.ItemsAsync()).ItemOf(kit.MatrixId);

        (await org.TeamHead.Client.ItemsAsync()).ItemFor(kit.MatrixId).Kinds().Should().Equal("OwnVersionInApproval");
        (await org.DepartmentHead.Client.ItemsAsync()).ItemFor(kit.MatrixId).Kinds().Should().Equal("VersionAwaitsHeadApproval");
        (hr is not null).Should().Be(isShown);
        if (hr is not null)
        {
            hr.Kinds().Should().Equal("VersionStuckAtHead");
            hr["isReference"]!.GetValue<bool>().Should().BeTrue();
            hr["group"]!.GetValue<string>().Should().Be("StuckVersions");
            hr.FirstReason()["details"]!["person"]!["id"]!.GetValue<Guid>().Should().Be(org.DepartmentHead.Employee);
            (await org.Hr.Client.DeskAsync())["actionCount"]!.GetValue<int>().Should().Be(await org.Hr.Client.ActionItemCountAsync());
        }
    }

    [Fact]
    public async Task Ac113_Ac115_TheDeskAnswersAboutTheSignedInUserAlone_WhateverTheRequestNames()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await MatrixKit.CreateAsync(org);

        var own = await org.TeamHead.Client.GetAsync($"/api/v1/desk/items?userId={org.DepartmentHead.Id}&employeeId={org.DepartmentHead.Employee}&limit=2000");
        var other = await org.DepartmentHead.Client.GetAsync($"/api/v1/desk/items?userId={org.TeamHead.Id}&limit=2000");

        own.Expect(HttpStatusCode.OK).Json!["items"]!.AsArray().Select(item => item!["object"]!["id"]!.GetValue<Guid>()).Should().Contain(kit.MatrixId);
        other.Expect(HttpStatusCode.OK).Json!["items"]!.AsArray().Select(item => item!["object"]!["id"]!.GetValue<Guid>()).Should().NotContain(kit.MatrixId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/items")]
    [InlineData("/count")]
    public async Task Ac113_TheDesk_IsRefusedToASignedOutCaller(string route)
    {
        (await fixture.Host.Anonymous().GetAsync($"/api/v1/desk{route}")).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ac17_TheCounter_IsTheNumberOfItemsThatAreActions_ThoseForReferenceExcluded_AndOneItemWithSeveralReasonsCountsOnce()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);

        foreach (var member in new[] { org.TeamHead, org.DepartmentHead, org.Hr, org.RootHead })
        {
            var desk = await member.Client.DeskAsync();
            var items = await member.Client.ItemsAsync();
            var counter = desk["actionCount"]!.GetValue<int>();
            counter.Should().Be(items.Count(item => !item["isReference"]!.GetValue<bool>()));
            counter.Should().Be(await member.Client.CountAsync());
            desk["groups"]!.AsArray().Where(group => !group!["isReference"]!.GetValue<bool>()).Sum(group => group!["total"]!.GetValue<int>()).Should().Be(counter);
        }
    }
}
