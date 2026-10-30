using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// What the commission sees of a candidate's earlier applications on the review form: the decided ones of finished sessions with their decision and
/// rating, the change of the average of each competency since the last of them, and nothing of an application that was rejected at the formal check.
/// </summary>
public sealed class PromotionHistoryTests(PromotionHost fixture) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";

    [Fact]
    public async Task Ac84_TheReviewOfASecondApplication_ShowsTheDecidedFirstOneAndTheChangeOfTheAverages_ButNotARejectedOne()
    {
        var first = await fixture.NewSceneAsync();
        var raters = await first.NewRatersAsync(3, 2);
        var returning = await first.NewCandidateAsync();
        var formerlyRejected = await first.NewCandidateAsync();
        var firstApplication = await first.SubmitForAdmissionAsync(returning, raters);
        var rejectedApplication = await first.SubmitAsync(formerlyRejected, raters);
        await first.PutInStageAsync(3);
        await first.RateAsync(raters, firstApplication.Id, 4, 3);
        await first.PutInStageAsync(5);
        var chair = first.Org.TeamHead.Client;
        var firstReview = (await chair.GetAsync($"{Applications}/{firstApplication.Id}/review")).Expect(HttpStatusCode.OK);
        (await chair.PutAsync($"{Applications}/{firstApplication.Id}/review", first.ReviewBody(4, 3, "Meets"), firstReview.ETag)).Expect(HttpStatusCode.OK);
        await first.PutInStageAsync(7);
        (await first.StatusNowAsync(formerlyRejected, rejectedApplication.Id)).Should().Be("FormalRejected");

        var second = await first.AnnounceNextAsync();
        var secondApplication = await second.SubmitForAdmissionAsync(returning, raters);
        var otherApplication = await second.SubmitForAdmissionAsync(formerlyRejected, raters);
        await second.PutInStageAsync(3);
        await second.RateAsync(raters, secondApplication.Id, 3, 3);
        await second.RateAsync(raters, otherApplication.Id, 3, 3);
        await second.PutInStageAsync(5);

        var review = (await chair.GetAsync($"{Applications}/{secondApplication.Id}/review")).Expect(HttpStatusCode.OK).Json!;
        var withoutHistory = (await chair.GetAsync($"{Applications}/{otherApplication.Id}/review")).Expect(HttpStatusCode.OK).Json!;

        var history = review["history"]!.AsArray().Should().ContainSingle().Subject!;
        history["waveId"]!.GetValue<Guid>().Should().Be(first.WaveId);
        history["decision"]!.GetValue<string>().Should().Be("Meets");
        history["rating"]!.GetValue<decimal>().Should().Be(87.5m);
        history["failedCritical"]!.AsArray().Should().BeEmpty();
        ChangeOf(review, first.Kit.Critical.Id).Should().Be((false, 4m, -1m));
        ChangeOf(review, first.Kit.Profile.Id).Should().Be((false, 3m, 0m), "unchanged");
        review["readinessChange"]!["delta"]!.GetValue<decimal>().Should().Be(-12.5m);
        withoutHistory["history"]!.AsArray().Should().BeEmpty("the rejected application of the first session does not count");
        withoutHistory["readinessChange"].Should().BeNull();
        withoutHistory["competencies"]!.AsArray().Should().OnlyContain(competency => competency!["change"] == null);
    }

    private static (bool IsNew, decimal? Previous, decimal? Delta) ChangeOf(System.Text.Json.Nodes.JsonNode review, Guid competencyId)
    {
        var change = review["competencies"]!.AsArray().Single(competency => competency!["competencyId"]!.GetValue<Guid>() == competencyId)!["change"]!;
        return (change["isNew"]!.GetValue<bool>(), change["previousAverage"]?.GetValue<decimal>(), change["delta"]?.GetValue<decimal>());
    }
}
