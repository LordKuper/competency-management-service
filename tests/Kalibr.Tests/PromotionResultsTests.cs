using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The end of a session for the people it concerns: the chair who changes a decision until the commission stage ends, the application left without a
/// decision and the HR told of it, the letters that publish the results to everyone but those whose application fell out, and the letter at the end
/// of the feedback stage that names those who did not mark the feedback.
/// </summary>
public sealed class PromotionResultsTests(PromotionHost fixture, TestEnvironment environment) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";

    [Fact]
    public async Task Ac32_Ac70_Ac36_TheChairChangesADecisionUntilTheStageEnds_ThenTheUndecidedAreListedForTheHr_TheResultsAreMailed_AndTheUnmarkedFeedbackIsReported()
    {
        var scene = await fixture.NewSceneAsync();
        var hr = scene.Org.Hr.User.Email;
        var raters = await scene.NewRatersAsync(3, 2);
        var decidedCandidate = await scene.NewCandidateAsync();
        var undecidedCandidate = await scene.NewCandidateAsync();
        var rejectedCandidate = await scene.NewCandidateAsync();
        var withdrawnCandidate = await scene.NewCandidateAsync();
        var decided = await scene.SubmitForAdmissionAsync(decidedCandidate, raters);
        var undecided = await scene.SubmitForAdmissionAsync(undecidedCandidate, raters);
        var rejected = await scene.SubmitAsync(rejectedCandidate, raters);
        var withdrawn = await scene.SubmitAsync(withdrawnCandidate, raters);
        (await withdrawnCandidate.Client.PostAsync($"{Applications}/{withdrawn.Id}/withdraw", ifMatch: withdrawn.ETag)).Expect(HttpStatusCode.OK);
        await scene.PutInStageAsync(3);
        await scene.RateAsync(raters, decided.Id, 4, 4);
        await scene.RateAsync(raters, undecided.Id, 4, 4);

        await scene.PutInStageAsync(5);

        var chair = scene.Org.TeamHead.Client;
        var review = (await chair.GetAsync($"{Applications}/{decided.Id}/review")).Expect(HttpStatusCode.OK);
        var first = (await chair.PutAsync($"{Applications}/{decided.Id}/review", scene.ReviewBody(4, 4, "Meets"), review.ETag)).Expect(HttpStatusCode.OK);
        var changed = (await chair.PutAsync($"{Applications}/{decided.Id}/review", scene.ReviewBody(4, 4, "DoesNotMeet"), first.ETag)).Expect(HttpStatusCode.OK);
        await scene.PutInStageAsync(6);

        changed.Json!["decision"]!.GetValue<string>().Should().Be("DoesNotMeet", "the chair changes the decision until the stage ends");
        (await scene.StatusNowAsync(undecidedCandidate, undecided.Id)).Should().Be("NoDecision");
        (await scene.StatusNowAsync(decidedCandidate, decided.Id)).Should().Be("Decided");
        (await scene.StatusNowAsync(rejectedCandidate, rejected.Id)).Should().Be("FormalRejected");
        var decidedName = decided.Json!["candidateName"]!.GetValue<string>();
        var undecidedName = undecided.Json!["candidateName"]!.GetValue<string>();
        var unsettled = (await environment.Mail.WaitForContainingAsync(hr, "не принято решение")).Should().ContainSingle().Subject;
        unsettled.Text.Should().Contain(undecidedName).And.NotContain(decidedName);
        foreach (var published in new[] { decidedCandidate, undecidedCandidate })
        {
            (await environment.Mail.WaitForContainingAsync(published.User.Email, "опубликованы")).Should().ContainSingle();
        }

        foreach (var fellOut in new[] { rejectedCandidate, withdrawnCandidate })
        {
            (await environment.Mail.FindContainingAsync(fellOut.User.Email, "опубликованы")).Should().BeEmpty("a rejected or withdrawn application has no result");
        }

        var seen = (await scene.GetApplicationAsync(decidedCandidate.Client, decided.Id)).Expect(HttpStatusCode.OK);
        (await decidedCandidate.Client.PostAsync($"{Applications}/{decided.Id}/feedback", ifMatch: seen.ETag)).Expect(HttpStatusCode.OK);
        await scene.PutInStageAsync(7);

        foreach (var recipient in new[] { hr, scene.Org.TeamHead.User.Email })
        {
            var report = (await environment.Mail.WaitForContainingAsync(recipient, "Не отметили получение обратной связи")).Should().ContainSingle().Subject;
            report.Text.Should().Contain(undecidedName).And.NotContain(decidedName);
        }
    }
}
