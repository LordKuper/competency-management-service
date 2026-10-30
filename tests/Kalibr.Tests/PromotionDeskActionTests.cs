using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The expected actions of a session on the desk, read from the server: the HR who enters the dates, the head who checks the raters, the rater who has
/// to rate, the candidate who owes artifacts and then marks the feedback, and the chair who decides. Each item stands while its action is due and goes when it is done.
/// </summary>
public sealed class PromotionDeskActionTests(PromotionHost fixture) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";

    private static async Task<string[]?> KindsAsync(Member member, Guid objectId) => (await member.Client.ItemsAsync()).ItemOf(objectId)?.Kinds();

    [Fact]
    public async Task Ac50_EachRoleIsAskedForItsActionAtItsStage_AndTheItemGoesWhenTheActionIsDone()
    {
        var scene = await fixture.NewSceneAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var candidate = await scene.NewCandidateAsync();
        var submitted = await scene.SubmitAsync(candidate, raters);
        var (hr, head, rater) = (scene.Org.Hr, scene.Org.TeamHead, raters[0]);
        var id = submitted.Id;

        var hrAsked = await KindsAsync(hr, scene.WaveId);
        var headAsked = await KindsAsync(head, id);
        (await hr.Client.PutAsync($"{Applications}/{id}/formal", new { hiredOrPromotedOn = new DateOnly(2020, 1, 1), ignoreInterval = false }, submitted.ETag)).Expect(HttpStatusCode.OK);

        hrAsked.Should().Equal("PromotionFormalCheckPending");
        headAsked.Should().Equal("PromotionRatersReview");
        (await KindsAsync(hr, scene.WaveId)).Should().BeNull("the date is entered");
        (await KindsAsync(rater, id)).Should().BeNull("the application is not admitted yet");

        await scene.PutInStageAsync(3);

        var raterAsked = await KindsAsync(rater, id);
        await scene.RateAsync([rater], id, 2, 4);
        raterAsked.Should().Equal("PromotionRatingRequested");
        (await KindsAsync(rater, id)).Should().BeNull("the rater has answered");
        await scene.RateAsync(raters[1..], id, 2, 4);

        await scene.PutInStageAsync(4);

        var owing = (await scene.GetApplicationAsync(candidate.Client, id)).Expect(HttpStatusCode.OK);
        var candidateAsked = await KindsAsync(candidate, id);
        var link = new { description = "Что сделано", url = "https://docs.test.local/a" };
        (await candidate.Client.PutAsync($"{Applications}/{id}/artifacts", new { artifacts = new[] { new { competencyId = scene.Kit.Critical.Id, links = new[] { link } } } }, owing.ETag))
            .Expect(HttpStatusCode.OK);
        candidateAsked.Should().Equal("PromotionArtifactsRequired");
        (await KindsAsync(candidate, id)).Should().BeNull("the mandatory artifact is attached");

        await scene.PutInStageAsync(5);

        var chairAsked = await KindsAsync(head, id);
        var review = (await head.Client.GetAsync($"{Applications}/{id}/review")).Expect(HttpStatusCode.OK);
        (await head.Client.PutAsync($"{Applications}/{id}/review", scene.ReviewBody(3, 4, "Meets"), review.ETag)).Expect(HttpStatusCode.OK);
        chairAsked.Should().Equal("PromotionDecisionsPending");
        (await KindsAsync(head, id)).Should().BeNull("the decision is recorded");

        await scene.PutInStageAsync(6);

        var feedbackAsked = await KindsAsync(candidate, id);
        var published = (await scene.GetApplicationAsync(candidate.Client, id)).Expect(HttpStatusCode.OK);
        (await candidate.Client.PostAsync($"{Applications}/{id}/feedback", ifMatch: published.ETag)).Expect(HttpStatusCode.OK);
        feedbackAsked.Should().Equal("PromotionFeedbackPending");
        (await KindsAsync(candidate, id)).Should().BeNull("the feedback is marked");
    }
}
