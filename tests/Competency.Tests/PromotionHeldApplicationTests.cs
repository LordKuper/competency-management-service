using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A unit whose commission has no chair who acts holds back its own applications and nothing else: the session goes on for the other units, the held
/// applications reach the commission once a chair is named, and those still held when the commission stage ends are left without a decision.
/// </summary>
public sealed class PromotionHeldApplicationTests(PromotionHost fixture, TestEnvironment environment) : IClassFixture<PromotionHost>
{
    private sealed record Session(PromotionScene Scene, Member Team, Member Sibling, ApiResponse TeamApplication, ApiResponse SiblingApplication);

    private async Task<Session> RatedSessionWithoutSiblingChairAsync()
    {
        var scene = await fixture.NewSceneAsync(withSibling: true);
        var raters = await scene.NewRatersAsync(3, 2);
        var team = await scene.NewCandidateAsync();
        var sibling = await scene.NewCandidateAsync(scene.Sibling!);
        var teamApplication = await scene.SubmitForAdmissionAsync(team, raters);
        var siblingApplication = await scene.SubmitForAdmissionAsync(sibling, raters, scene.Sibling);
        await scene.BlockAsync(scene.Org.SiblingHead);
        await scene.PutInStageAsync(3);
        await scene.RateAsync(raters, teamApplication.Id, 4, 4);
        await scene.RateAsync(raters, siblingApplication.Id, 4, 4, scene.Sibling);
        return new Session(scene, team, sibling, teamApplication, siblingApplication);
    }

    [Fact]
    public async Task Ac66_AnApplicationOfAUnitWithoutAChair_StaysAdmittedWhileTheOtherUnitsGoOn_AndReachesTheNewChairWithALetter()
    {
        var session = await RatedSessionWithoutSiblingChairAsync();
        var scene = session.Scene;

        await scene.PutInStageAsync(5);

        (await scene.StatusNowAsync(session.Team, session.TeamApplication.Id)).Should().Be("UnderReview", "the unit with a chair is not held");
        (await scene.StatusNowAsync(session.Sibling, session.SiblingApplication.Id)).Should().Be("Admitted", "the unit without a chair holds its applications");

        var newChair = scene.Org.DepartmentHead;
        (await PromotionScene.SaveCommissionAsync(scene.Org.SiblingHr.Client, scene.Org.Sibling.Id, [newChair.Employee], newChair.Employee)).Expect(HttpStatusCode.OK);
        await Waiting.UntilAsync(
            async () => await scene.StatusNowAsync(session.Sibling, session.SiblingApplication.Id) == "UnderReview",
            "the held application reached the commission of the unit with a chair",
            TimeSpan.FromSeconds(60));

        (await environment.Mail.WaitForContainingAsync(newChair.User.Email, $"promotion/commission/{scene.WaveId}/{scene.Org.Sibling.Id}")).Should().NotBeEmpty("the new chair is told of the application that arrived");
        var review = (await newChair.Client.GetAsync($"/api/v1/promotion/applications/{session.SiblingApplication.Id}/review")).Expect(HttpStatusCode.OK);
        review.Json!["artifactsMissing"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task Ac66_AnApplicationHeldForWantOfAChairToTheEnd_GetsNoDecisionWhileTheOthersAreDecided()
    {
        var session = await RatedSessionWithoutSiblingChairAsync();
        var scene = session.Scene;
        await scene.PutInStageAsync(5);
        var chair = scene.Org.TeamHead.Client;
        var review = (await chair.GetAsync($"/api/v1/promotion/applications/{session.TeamApplication.Id}/review")).Expect(HttpStatusCode.OK);
        (await chair.PutAsync($"/api/v1/promotion/applications/{session.TeamApplication.Id}/review", scene.ReviewBody(4, 4, "Meets"), review.ETag)).Expect(HttpStatusCode.OK);

        await scene.PutInStageAsync(6);

        (await scene.StatusNowAsync(session.Team, session.TeamApplication.Id)).Should().Be("Decided");
        (await scene.StatusNowAsync(session.Sibling, session.SiblingApplication.Id)).Should().Be("NoDecision");
    }
}
