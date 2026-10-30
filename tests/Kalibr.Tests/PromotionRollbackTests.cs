using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Taking a session back to a stage it has passed: the warning that counts what resets or is recomputed, including the results the candidates stop seeing,
/// and the rollback itself, which is carried out only for those who may change the dates and moves the session to the chosen stage.
/// </summary>
public sealed class PromotionRollbackTests(PromotionHost fixture) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";

    private static Dictionary<string, int> Effects(ApiResponse preview) =>
        preview.Json!["effects"]!.AsArray().ToDictionary(effect => effect!["kind"]!.GetValue<string>(), effect => effect!["count"]!.GetValue<int>());

    [Fact]
    public async Task Ac54_TheRollbackWarnsWhatItResets_AndWhenConfirmedPutsTheSessionBackAndHidesThePublishedResults()
    {
        var scene = await fixture.NewSceneAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var decidedCandidate = await scene.NewCandidateAsync();
        var rejectedCandidate = await scene.NewCandidateAsync();
        var decided = await scene.SubmitForAdmissionAsync(decidedCandidate, raters);
        await scene.SubmitAsync(rejectedCandidate, raters);
        await scene.PutInStageAsync(3);
        await scene.RateAsync(raters, decided.Id, 4, 4);
        await scene.PutInStageAsync(5);
        var chair = scene.Org.TeamHead.Client;
        var review = (await chair.GetAsync($"{Applications}/{decided.Id}/review")).Expect(HttpStatusCode.OK);
        (await chair.PutAsync($"{Applications}/{decided.Id}/review", scene.ReviewBody(4, 4, "Meets"), review.ETag)).Expect(HttpStatusCode.OK);
        await scene.PutInStageAsync(6);
        var route = $"/api/v1/promotion/waves/{scene.WaveId}/rollback";

        var toFormalCheck = (await scene.Org.Hr.Client.GetAsync($"{route}?stage=2")).Expect(HttpStatusCode.OK);
        var toReview = (await scene.Org.Hr.Client.GetAsync($"{route}?stage=5")).Expect(HttpStatusCode.OK);
        var toCurrent = await scene.Org.Hr.Client.GetAsync($"{route}?stage=6");
        var byPlain = await scene.Org.Plain.Client.GetAsync($"{route}?stage=5");

        Effects(toFormalCheck).Should().Equal(new Dictionary<string, int>
        {
            ["FormalAdmittedReset"] = 1,
            ["FormalRejectedReset"] = 1,
            ["RatingRecalculated"] = 1,
            ["ArtifactsRecalculated"] = 1,
            ["ResultsHidden"] = 1,
        });
        Effects(toReview).Should().Equal(new Dictionary<string, int> { ["ResultsHidden"] = 1 }, "only the published result of the one decided application is hidden");
        toReview.Json!["letterStages"]!.AsArray().Select(stage => stage!.GetValue<int>()).Should().Equal(5);
        toCurrent.Status.Should().Be(HttpStatusCode.BadRequest, "only a stage already passed can be chosen");
        byPlain.Status.Should().Be(HttpStatusCode.Forbidden);

        var wave = (await scene.Org.Hr.Client.GetAsync($"/api/v1/promotion/waves/{scene.WaveId}")).Expect(HttpStatusCode.OK);
        var ends = wave.Json!["endsOn"]!.AsArray().Select(day => DateOnly.Parse(day!.GetValue<string>())).ToArray();
        var newEnds = new[] { ends[0], ends[1], ends[2], ends[3], PromotionScene.Today.AddDays(3), PromotionScene.Today.AddDays(6) };
        var byPlainRollback = await scene.Org.Plain.Client.PostAsync(route, new { stage = 5, endsOn = newEnds }, wave.ETag);
        var rolledBack = (await scene.Org.Hr.Client.PostAsync(route, new { stage = 5, endsOn = newEnds }, wave.ETag)).Expect(HttpStatusCode.OK);

        byPlainRollback.Status.Should().Be(HttpStatusCode.Forbidden);
        rolledBack.Json!["stage"]!.GetValue<int>().Should().Be(5);
        var candidateView = (await scene.GetApplicationAsync(decidedCandidate.Client, decided.Id)).Expect(HttpStatusCode.OK);
        candidateView.Json!["result"].Should().BeNull("the candidate no longer sees the result until it is published again");
        PromotionScene.StatusOf(candidateView).Should().Be("UnderReview");
        var again = (await chair.GetAsync($"{Applications}/{decided.Id}/review")).Expect(HttpStatusCode.OK);
        (await chair.PutAsync($"{Applications}/{decided.Id}/review", scene.ReviewBody(4, 4, "DoesNotMeet"), again.ETag)).Expect(HttpStatusCode.OK);
    }
}
