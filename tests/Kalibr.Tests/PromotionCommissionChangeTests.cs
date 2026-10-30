using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Changing the commission of a unit while its applications are under review: who may do it, what the new and the removed members get at once,
/// and what the old chair's entries become.
/// </summary>
public sealed class PromotionCommissionChangeTests(PromotionHost fixture, TestEnvironment environment) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";

    private sealed record Review(PromotionScene Scene, Member Candidate, ApiResponse Application, string CommissionLink);

    private async Task<Review> SessionUnderReviewAsync()
    {
        var scene = await fixture.NewSceneAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var candidate = await scene.NewCandidateAsync();
        var application = await scene.SubmitForAdmissionAsync(candidate, raters);
        await scene.PutInStageAsync(3);
        await scene.RateAsync(raters, application.Id, 4, 4);
        await scene.PutInStageAsync(5);
        var link = $"promotion/commission/{scene.WaveId}/{scene.Org.Team.Id}";
        (await environment.Mail.WaitForContainingAsync(scene.Org.TeamHead.User.Email, link)).Should().ContainSingle("the chair is told of the application that arrived");
        return new Review(scene, candidate, application, link);
    }

    private static Task<ApiResponse> ReviewOfAsync(Member who, Guid applicationId) => who.Client.GetAsync($"{Applications}/{applicationId}/review");

    [Fact]
    public async Task Ac95_Ac63_AMemberAddedMidReview_SeesTheApplicationsAndIsToldOfThem_AndOneTakenOffSeesThemNoMore()
    {
        var review = await SessionUnderReviewAsync();
        var scene = review.Scene;
        var added = scene.Org.DepartmentHead;
        (await ReviewOfAsync(added, review.Application.Id)).Status.Should().Be(HttpStatusCode.NotFound, "an outsider to the commission sees nothing");

        (await PromotionScene.SaveCommissionAsync(scene.Org.Hr.Client, scene.Org.Team.Id, [added.Employee], scene.Org.TeamHead.Employee)).Expect(HttpStatusCode.OK);

        (await ReviewOfAsync(added, review.Application.Id)).Expect(HttpStatusCode.OK);
        (await environment.Mail.WaitForContainingAsync(added.User.Email, review.CommissionLink)).Should().ContainSingle();
        (await environment.Mail.FindContainingAsync(scene.Org.TeamHead.User.Email, review.CommissionLink)).Should().ContainSingle("a member who sat already is not told again");

        (await PromotionScene.SaveCommissionAsync(scene.Org.Hr.Client, scene.Org.Team.Id, [], scene.Org.TeamHead.Employee)).Expect(HttpStatusCode.OK);

        (await ReviewOfAsync(added, review.Application.Id)).Status.Should().Be(HttpStatusCode.NotFound, "a member taken off loses the review at once");
        (await ReviewOfAsync(scene.Org.TeamHead, review.Application.Id)).Expect(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac95_TheChairReplacedMidReview_LeavesTheOldChairsScoresAndDecisionStanding_AndTheEntriesToTheNewChair()
    {
        var review = await SessionUnderReviewAsync();
        var scene = review.Scene;
        var oldChair = scene.Org.TeamHead;
        var newChair = scene.Org.DepartmentHead;
        var opened = (await ReviewOfAsync(oldChair, review.Application.Id)).Expect(HttpStatusCode.OK);
        var decided = (await oldChair.Client.PutAsync($"{Applications}/{review.Application.Id}/review", scene.ReviewBody(3, 2, "Meets"), opened.ETag)).Expect(HttpStatusCode.OK);

        (await PromotionScene.SaveCommissionAsync(scene.Org.Admin, scene.Org.Team.Id, [newChair.Employee], newChair.Employee)).Expect(HttpStatusCode.OK);

        var kept = (await ReviewOfAsync(newChair, review.Application.Id)).Expect(HttpStatusCode.OK);
        kept.Json!["decision"]!.GetValue<string>().Should().Be("Meets");
        kept.Json["isRatingFromFinalScores"]!.GetValue<bool>().Should().BeTrue();
        kept.Json["rating"]!.GetValue<decimal>().Should().Be(decided.Json!["rating"]!.GetValue<decimal>());
        (await oldChair.Client.PutAsync($"{Applications}/{review.Application.Id}/review", scene.ReviewBody(4, 4, "DoesNotMeet"), kept.ETag)).Status
            .Should().Be(HttpStatusCode.Forbidden, "the old chair is a member now and decides no more");
        var changed = (await newChair.Client.PutAsync($"{Applications}/{review.Application.Id}/review", scene.ReviewBody(4, 4, "DoesNotMeet"), kept.ETag)).Expect(HttpStatusCode.OK);
        changed.Json!["decision"]!.GetValue<string>().Should().Be("DoesNotMeet");
    }

    [Fact]
    public async Task Ac95_TheCommissionOfAUnitInASessionIsChangedByItsHrAndTheAdministratorAlone()
    {
        var review = await SessionUnderReviewAsync();
        var scene = review.Scene;
        var member = scene.Org.DepartmentHead;
        (await PromotionScene.SaveCommissionAsync(scene.Org.Admin, scene.Org.Team.Id, [member.Employee], scene.Org.TeamHead.Employee)).Expect(HttpStatusCode.OK);
        var route = $"/api/v1/promotion/commissions/{scene.Org.Team.Id}";
        var current = (await scene.Org.Admin.GetAsync(route)).Expect(HttpStatusCode.OK);
        var body = new { memberEmployeeIds = new[] { member.Employee }, chairEmployeeId = (Guid?)member.Employee };

        var byMember = await member.Client.PutAsync(route, body, current.ETag);
        var byTheChair = await scene.Org.TeamHead.Client.PutAsync(route, body, current.ETag);
        var byAnotherHr = await scene.Org.SiblingHr.Client.PutAsync(route, body, current.ETag);
        var byPlain = await scene.Org.Plain.Client.PutAsync(route, body, current.ETag);
        var unchanged = (await scene.Org.Admin.GetAsync(route)).Expect(HttpStatusCode.OK);
        var byHr = await scene.Org.Hr.Client.PutAsync(route, body, current.ETag);

        byMember.Status.Should().Be(HttpStatusCode.Forbidden);
        byTheChair.Status.Should().Be(HttpStatusCode.Forbidden);
        byAnotherHr.Status.Should().Be(HttpStatusCode.NotFound, "the HR of another unit does not see this commission");
        byPlain.Status.Should().Be(HttpStatusCode.NotFound);
        unchanged.Json!["chairEmployeeId"]!.GetValue<Guid>().Should().Be(scene.Org.TeamHead.Employee, "no refused change touched the commission");
        byHr.Expect(HttpStatusCode.OK);
    }
}
