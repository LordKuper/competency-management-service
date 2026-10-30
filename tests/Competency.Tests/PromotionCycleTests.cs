using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// One career session lived through from the filed applications to the published results, the stages brought on by the scheduler of the host: the
/// formal check that rejects one application and admits another (AC-17), the letters of the rating stage (AC-62), the raters' answers that no one else
/// reads (AC-19, AC-20), the check after the rating (AC-21, AC-49, AC-80), the chair's scores and decision with the ranking only the commission sees
/// (AC-29 to AC-31), and what the candidate sees from the publication on (AC-68, AC-69).
/// </summary>
public sealed class PromotionCycleTests(PromotionHost fixture, TestEnvironment environment) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";

    [Fact]
    public async Task Ac17_Ac62_Ac19_Ac20_Ac21_Ac80_Ac29_Ac30_Ac31_Ac68_Ac69_TheSessionRunsFromTheFormalCheckToTheFeedback()
    {
        var scene = await fixture.SceneAsync();
        var chair = scene.Org.TeamHead;
        var admittedCandidate = await scene.NewCandidateAsync();
        var rejectedCandidate = await scene.NewCandidateAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var otherRaters = await scene.NewRatersAsync(3, 2);
        var admitted = await scene.SubmitAsync(admittedCandidate, raters);
        var rejected = await scene.SubmitAsync(rejectedCandidate, otherRaters);
        (await scene.Org.Hr.Client.PutAsync($"{Applications}/{admitted.Id}/formal", new { hiredOrPromotedOn = new DateOnly(2020, 1, 1), ignoreInterval = false }, admitted.ETag))
            .Expect(HttpStatusCode.OK);

        await scene.PutInStageAsync(3);

        var afterFormal = (await scene.GetApplicationAsync(admittedCandidate.Client, admitted.Id)).Expect(HttpStatusCode.OK);
        var afterRejection = (await scene.GetApplicationAsync(rejectedCandidate.Client, rejected.Id)).Expect(HttpStatusCode.OK);
        ScoreStatus(afterFormal).Should().Be("Admitted", "AC-17: the date entered and the conditions met");
        ScoreStatus(afterRejection).Should().Be("FormalRejected", "AC-17: no date entered");
        afterRejection.Json!["formalFailures"]!.AsArray().Should().NotBeEmpty();
        (await WaitForLetterAsync(rejectedCandidate, $"promotion/applications/{rejected.Id}")).Should().NotBeEmpty("the candidate is told of the rejection");
        (await WaitForLetterAsync(raters[0], "promotion/ratings")).Should().NotBeEmpty("AC-62: the admitted candidate's rater is asked to rate");
        (await environment.Mail.FindAsync(otherRaters[0].User.Email)).Where(mail => mail.Text.Contains("promotion/ratings")).Should().BeEmpty("AC-62: no letter for a rejected candidate's rater");

        var form = (await raters[0].Client.GetAsync($"{Applications}/{admitted.Id}/rating")).Expect(HttpStatusCode.OK);
        form.Json!["canEdit"]!.GetValue<bool>().Should().BeTrue();
        form.Json["answers"].Should().BeNull("a rater who has not answered has no answers");
        foreach (var (rater, index) in raters.Select((rater, index) => (rater, index)))
        {
            (await rater.Client.PutAsync($"{Applications}/{admitted.Id}/rating", Answers(scene, 4, index == 0 ? null : 4))).Expect(HttpStatusCode.OK);
        }

        var own = (await raters[0].Client.GetAsync($"{Applications}/{admitted.Id}/rating")).Expect(HttpStatusCode.OK);
        own.Json!["answers"]!.AsArray().Should().HaveCount(2, "AC-19: a rater sees his own answers");
        (await raters[0].Client.PutAsync($"{Applications}/{admitted.Id}/rating", Answers(scene, 4, 3))).Expect(HttpStatusCode.OK);
        (await admittedCandidate.Client.GetAsync($"{Applications}/{admitted.Id}/rating")).Status.Should().Be(HttpStatusCode.Forbidden);
        (await admittedCandidate.Client.PutAsync($"{Applications}/{admitted.Id}/rating", Answers(scene, 4, 4))).Status.Should().Be(HttpStatusCode.Forbidden);
        (await raters[0].Client.GetAsync($"{Applications}/{rejected.Id}/rating")).Status.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        (await raters[0].Client.GetAsync($"{Applications}/{admitted.Id}/review")).Status.Should().Be(HttpStatusCode.NotFound, "a rater has no role on the application, so its commission page is not there for him");

        await scene.PutInStageAsync(4);

        (await raters[0].Client.PutAsync($"{Applications}/{admitted.Id}/rating", Answers(scene, 1, 1))).Status.Should().Be(HttpStatusCode.Conflict, "the answers close with the stage");
        var tasks = (await raters[1].Client.GetAsync("/api/v1/promotion/ratings/mine")).Expect(HttpStatusCode.OK).Json!.AsArray();
        tasks.Single(task => task!["applicationId"]!.GetValue<Guid>() == admitted.Id)!["isRated"]!.GetValue<bool>().Should().BeTrue();

        await scene.PutInStageAsync(5);

        var review = (await chair.Client.GetAsync($"{Applications}/{admitted.Id}/review")).Expect(HttpStatusCode.OK);
        review.Json!["status"]!.GetValue<string>().Should().Be("UnderReview", "AC-80: the candidate who met the requirements reached the commission");
        review.Json["meetsAfterRating"]!.GetValue<bool>().Should().BeTrue("AC-21");
        review.Json["raterCounts"].Should().NotBeNull();
        AssertNobodyNamed(review.Body, raters, "the commission sees no authors of the ratings");
        (await admittedCandidate.Client.GetAsync($"{Applications}/{admitted.Id}/review")).Status.Should().Be(HttpStatusCode.Forbidden);
        var rankingRoute = $"/api/v1/promotion/waves/{scene.WaveId}/units/{scene.Org.Team.Id}/ranking";
        (await admittedCandidate.Client.GetAsync(rankingRoute)).Status.Should().BeOneOf([HttpStatusCode.Forbidden, HttpStatusCode.NotFound], "AC-30");
        (await rejectedCandidate.Client.GetAsync(rankingRoute)).Status.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);

        var decided = (await chair.Client.PutAsync($"{Applications}/{admitted.Id}/review", Review(scene, 4, 4, "Meets"), review.ETag)).Expect(HttpStatusCode.OK);
        var ranking = (await chair.Client.GetAsync(rankingRoute)).Expect(HttpStatusCode.OK).Json!.AsArray();
        var plain = await scene.Org.Plain.Client.GetAsync(rankingRoute);
        decided.Json!["status"]!.GetValue<string>().Should().Be("Decided");
        decided.Json["decision"]!.GetValue<string>().Should().Be("Meets");
        decided.Json["isRatingFromFinalScores"]!.GetValue<bool>().Should().BeTrue("AC-29: the rating is computed from the chair's scores");
        decided.Json["rating"]!.GetValue<decimal>().Should().Be(100m);
        ranking.Select(item => item!["applicationId"]!.GetValue<Guid>()).Should().Contain(admitted.Id).And.NotContain(rejected.Id);
        plain.Status.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        var duringReview = (await scene.GetApplicationAsync(admittedCandidate.Client, admitted.Id)).Expect(HttpStatusCode.OK);
        ScoreStatus(duringReview).Should().Be("UnderReview", "the decision is hidden from the candidate until the results are published");
        duringReview.Json!["result"].Should().BeNull();
        (await admittedCandidate.Client.PostAsync($"{Applications}/{admitted.Id}/feedback", ifMatch: duringReview.ETag)).Status.Should().Be(HttpStatusCode.Conflict, "feedback is marked from the publication");

        await scene.PutInStageAsync(6);

        var published = (await scene.GetApplicationAsync(admittedCandidate.Client, admitted.Id)).Expect(HttpStatusCode.OK);
        ScoreStatus(published).Should().Be("Decided", "AC-68");
        published.Json!["result"]!["decision"]!.GetValue<string>().Should().Be("Meets");
        published.Json["result"]!["competencies"]!.AsArray().Should().HaveCount(2);
        AssertNobodyNamed(published.Json["result"]!.ToJsonString(), raters, "the result tells the candidate no rater");
        published.Json["feedbackReceivedAt"].Should().BeNull();
        var feedback = (await admittedCandidate.Client.PostAsync($"{Applications}/{admitted.Id}/feedback", ifMatch: published.ETag)).Expect(HttpStatusCode.OK);
        var again = (await admittedCandidate.Client.PostAsync($"{Applications}/{admitted.Id}/feedback", ifMatch: feedback.ETag)).Expect(HttpStatusCode.OK);
        feedback.Json!["feedbackReceivedAt"].Should().NotBeNull("AC-69");
        again.Json!["feedbackReceivedAt"]!.GetValue<DateTimeOffset>().Should().BeCloseTo(feedback.Json["feedbackReceivedAt"]!.GetValue<DateTimeOffset>(), TimeSpan.FromMilliseconds(1), "marking again changes nothing");
        (await chair.Client.PutAsync($"{Applications}/{admitted.Id}/review", Review(scene, 4, 4, "Meets"), decided.ETag)).Status.Should().Be(HttpStatusCode.Conflict, "the chair's page is closed once the stage is over");
    }

    private static string ScoreStatus(ApiResponse application) => application.Json!["status"]!.GetValue<string>();

    private static object Answers(PromotionScene scene, int? critical, int? additional) => new
    {
        answers = new[] { new { competencyId = scene.Kit.Critical.Id, score = critical }, new { competencyId = scene.Kit.Profile.Id, score = additional } },
    };

    private static object Review(PromotionScene scene, int critical, int additional, string decision) => new
    {
        finalScores = new[] { new { competencyId = scene.Kit.Critical.Id, score = critical }, new { competencyId = scene.Kit.Profile.Id, score = additional } },
        decision,
    };

    private static void AssertNobodyNamed(string body, IEnumerable<Member> raters, string because)
    {
        foreach (var rater in raters)
        {
            body.Should().NotContain(rater.Employee.ToString(), because);
            body.Should().NotContain(rater.User.Email, because);
        }
    }

    private async Task<CapturedMail[]> WaitForLetterAsync(Member member, string route)
    {
        CapturedMail[] letters = [];
        await Waiting.UntilAsync(
            async () => (letters = [.. (await environment.Mail.FindAsync(member.User.Email)).Where(mail => mail.Text.Contains(route))]).Length > 0,
            $"the letter with {route} reached {member.User.Email}");
        return letters;
    }
}
