using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The artifact stage for a candidate whom the raters left short of the mandatory requirements: what the candidate must give, the limit of ten
/// links with a description each, sending the application to the commission at the candidate's word or letting the deadline do it, and the mark
/// the commission sees on an application that arrived without what it owed.
/// </summary>
public sealed class PromotionArtifactTests(PromotionHost fixture) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";

    private static object Links(Guid competencyId, params object[] links) => new { artifacts = new[] { new { competencyId, links } } };

    private static object Link(string url, string description = "Что сделано") => new { description, url };

    [Fact]
    public async Task Ac65_Ac85_Ac80_ACandidateShortOfTheRequirements_GivesArtifactsOrSendsTheApplication_AndTheCommissionSeesWhatWasMissing()
    {
        var scene = await fixture.NewSceneAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var passing = await scene.NewCandidateAsync();
        var giving = await scene.NewCandidateAsync();
        var sending = await scene.NewCandidateAsync();
        var idle = await scene.NewCandidateAsync();
        var applications = new Dictionary<Member, ApiResponse>();
        foreach (var candidate in new[] { passing, giving, sending, idle })
        {
            applications[candidate] = await scene.SubmitForAdmissionAsync(candidate, raters);
        }

        await scene.PutInStageAsync(3);
        await scene.RateAsync(raters, applications[passing].Id, 4, 4);
        foreach (var candidate in new[] { giving, sending, idle })
        {
            await scene.RateAsync(raters, applications[candidate].Id, 2, 4);
        }

        await scene.PutInStageAsync(4);

        var critical = scene.Kit.Critical.Id;
        var givingId = applications[giving].Id;
        var owing = (await scene.GetApplicationAsync(giving.Client, givingId)).Expect(HttpStatusCode.OK);
        (await scene.StatusNowAsync(passing, applications[passing].Id)).Should().Be("UnderReview", "the candidate who met the requirements went to the commission without artifacts");
        (await scene.StatusNowAsync(giving, givingId)).Should().Be("Admitted");
        owing.Json!["mandatoryArtifactCompetencyIds"]!.AsArray().Select(id => id!.GetValue<Guid>()).Should().BeEquivalentTo([critical], "only the competency below the requirement is mandatory");
        (await passing.Client.PostAsync($"{Applications}/{applications[passing].Id}/send-to-review", ifMatch: applications[passing].ETag)).Status.Should().Be(HttpStatusCode.Conflict, "a candidate who met the requirements has nothing to send");

        var eleven = await giving.Client.PutAsync($"{Applications}/{givingId}/artifacts", Links(critical, [.. Enumerable.Range(0, 11).Select(index => Link($"https://docs.test.local/{index}"))]), owing.ETag);
        var noLink = await giving.Client.PutAsync($"{Applications}/{givingId}/artifacts", Links(critical, Link("not a link")), owing.ETag);
        var wrongScheme = await giving.Client.PutAsync($"{Applications}/{givingId}/artifacts", Links(critical, Link("ftp://docs.test.local/file")), owing.ETag);
        var noDescription = await giving.Client.PutAsync($"{Applications}/{givingId}/artifacts", Links(critical, Link("https://docs.test.local/a", " ")), owing.ETag);
        var ten = (await giving.Client.PutAsync($"{Applications}/{givingId}/artifacts", Links(critical, [.. Enumerable.Range(0, 10).Select(index => Link($"https://docs.test.local/{index}"))]), owing.ETag))
            .Expect(HttpStatusCode.OK);

        foreach (var refused in new[] { eleven, noLink, wrongScheme, noDescription })
        {
            refused.Status.Should().Be(HttpStatusCode.BadRequest);
            refused.FieldErrors("artifacts").Should().NotBeEmpty();
        }

        ten.Json!["artifacts"]!.AsArray().Single()!["links"]!.AsArray().Should().HaveCount(10);
        ten.Json["missingArtifactCompetencyIds"]!.AsArray().Should().BeEmpty("one artifact attaches the competency");

        var sentWithArtifacts = (await giving.Client.PostAsync($"{Applications}/{givingId}/send-to-review", ifMatch: ten.ETag)).Expect(HttpStatusCode.OK);
        var bare = (await scene.GetApplicationAsync(sending.Client, applications[sending].Id)).Expect(HttpStatusCode.OK);
        var sentBare = (await sending.Client.PostAsync($"{Applications}/{bare.Id}/send-to-review", ifMatch: bare.ETag)).Expect(HttpStatusCode.OK);
        var changedAfterSending = await giving.Client.PutAsync($"{Applications}/{givingId}/artifacts", Links(critical, Link("https://docs.test.local/late")), sentWithArtifacts.ETag);

        sentWithArtifacts.Json!["isSentToReview"]!.GetValue<bool>().Should().BeTrue();
        PromotionScene.StatusOf(sentWithArtifacts).Should().Be("UnderReview");
        PromotionScene.StatusOf(sentBare).Should().Be("UnderReview");
        changedAfterSending.Status.Should().Be(HttpStatusCode.Conflict, "from sending on the artifacts cannot change");
        (await scene.StatusNowAsync(idle, applications[idle].Id)).Should().Be("Admitted", "the deadline has not come");

        await scene.PutInStageAsync(5);

        (await scene.StatusNowAsync(idle, applications[idle].Id)).Should().Be("UnderReview", "the deadline hands it over");
        (await MissingMarkAsync(scene, applications[passing].Id)).Should().BeFalse("it owed nothing");
        (await MissingMarkAsync(scene, givingId)).Should().BeFalse("it gave an artifact for the competency it owed");
        (await MissingMarkAsync(scene, applications[sending].Id)).Should().BeTrue("it was sent without the artifact it owed");
        (await MissingMarkAsync(scene, applications[idle].Id)).Should().BeTrue("it reached the commission at the deadline without the artifact it owed");
    }

    private static async Task<bool> MissingMarkAsync(PromotionScene scene, Guid applicationId) =>
        (await scene.Org.TeamHead.Client.GetAsync($"{Applications}/{applicationId}/review")).Expect(HttpStatusCode.OK).Json!["artifactsMissing"]!.GetValue<bool>();
}
