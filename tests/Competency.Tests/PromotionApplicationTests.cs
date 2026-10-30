using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// An application for a career transition while the session takes applications: who may file it, the target it may name, a draft that is
/// changed and deleted, the raters it must have, what a submitted one allows, who sees it, and the dates the responsible HR enters.
/// </summary>
public sealed class PromotionApplicationTests(PromotionHost fixture) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";

    [Fact]
    public async Task Ac47_Ac77_ACandidateOnTheTrackMayTargetOnlyTheNextLevel_AndTheDraftShowsTheServersPreliminaryRating()
    {
        var scene = await fixture.SceneAsync();
        var candidate = await scene.NewCandidateAsync();
        var raters = await scene.NewRatersAsync(1, 0);

        var sameLevel = await candidate.Client.PostAsync(Applications, scene.ApplicationBody(raters, scene.Kit.Junior.Id));
        var draft = (await candidate.Client.PostAsync(Applications, scene.ApplicationBody(raters))).Expect(HttpStatusCode.Created);

        sameLevel.Status.Should().Be(HttpStatusCode.BadRequest);
        sameLevel.FieldErrors("levelId").Should().NotBeEmpty();
        draft.Json!["status"]!.GetValue<string>().Should().Be("Draft");
        draft.Json["target"]!["levelId"]!.GetValue<Guid>().Should().Be(scene.Kit.Senior.Id);
        var rating = draft.Json["preliminaryRating"]!;
        rating["rating"]!.GetValue<decimal>().Should().Be(62.5m, "the critical competency scored 3 of 4 and the other 2 of 4, equal parts of the one group");
        rating["unscoredCount"]!.GetValue<int>().Should().Be(0);
        rating["areCriticalMet"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Ac8_AnAccountBoundToNoEmployee_CannotFileAnApplication_AndNoOneFilesForAnother()
    {
        var scene = await fixture.SceneAsync();
        var raters = await scene.NewRatersAsync(1, 0);

        var unbound = await scene.Org.Plain.Client.PostAsync(Applications, scene.ApplicationBody(raters));

        unbound.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ac59_ADraftIsChangedAndDeleted_ByItsCandidateAlone()
    {
        var scene = await fixture.SceneAsync();
        var candidate = await scene.NewCandidateAsync();
        var other = await scene.NewCandidateAsync();
        var raters = await scene.NewRatersAsync(2, 0);
        var draft = (await candidate.Client.PostAsync(Applications, scene.ApplicationBody(raters[..1]))).Expect(HttpStatusCode.Created);

        var changed = (await candidate.Client.PutAsync($"{Applications}/{draft.Id}", scene.ApplicationBody(raters, additionalScore: 4), draft.ETag)).Expect(HttpStatusCode.OK);
        var byOther = await other.Client.PutAsync($"{Applications}/{draft.Id}", scene.ApplicationBody(raters), changed.ETag);
        var deleted = await candidate.Client.DeleteAsync($"{Applications}/{draft.Id}", changed.ETag);

        changed.Json!["raters"]!.AsArray().Should().HaveCount(2);
        changed.Json["preliminaryRating"]!["rating"]!.GetValue<decimal>().Should().Be(87.5m);
        byOther.Status.Should().Be(HttpStatusCode.NotFound);
        deleted.Status.Should().Be(HttpStatusCode.NoContent);
        (await scene.GetApplicationAsync(candidate.Client, draft.Id)).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac58_AnApplicationIsSubmittedWithFiveRatersOfWhomTwoAreFromAnotherUnit()
    {
        var scene = await fixture.SceneAsync();
        var candidate = await scene.NewCandidateAsync();
        var fewer = await scene.NewRatersAsync(2, 2);
        var allInside = await scene.NewRatersAsync(5, 0);
        var enough = await scene.NewRatersAsync(3, 2);

        var tooFew = await RefusedSubmitAsync(candidate, scene, fewer);
        var inside = await RefusedSubmitAsync(candidate, scene, allInside);
        var submitted = await scene.SubmitAsync(candidate, enough);

        tooFew.FieldErrors("raters").Should().ContainSingle().Which.Should().Contain("не меньше 5");
        inside.FieldErrors("raters").Should().ContainSingle().Which.Should().Contain("из других подразделений");
        PromotionScene.StatusOf(submitted).Should().Be("Submitted");
    }

    [Fact]
    public async Task Ac10_Ac78_TheEducationOfASubmittedApplication_UpdatesTheCardOfTheEmployee()
    {
        var scene = await fixture.SceneAsync();
        var candidate = await scene.NewCandidateAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var withoutEducation = await candidate.Client.PostAsync(Applications, scene.ApplicationBody(raters, education: null));

        var submitted = await scene.SubmitAsync(candidate, raters);

        withoutEducation.Status.Should().Be(HttpStatusCode.BadRequest);
        withoutEducation.FieldErrors("education").Should().NotBeEmpty();
        (await scene.Org.Admin.GetAsync($"/api/v1/employees/{candidate.Employee}")).Expect(HttpStatusCode.OK).Json!["education"]!.GetValue<string>().Should().Be("Bachelor");
        submitted.Json!["education"]!.GetValue<string>().Should().Be("Bachelor");
    }

    [Fact]
    public async Task Ac59_Ac9_ASubmittedApplicationIsNotChanged_ItIsWithdrawnOnce_AndANewOneMayThenBeFiled()
    {
        var scene = await fixture.SceneAsync();
        var candidate = await scene.NewCandidateAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var submitted = await scene.SubmitAsync(candidate, raters);

        var edit = await candidate.Client.PutAsync($"{Applications}/{submitted.Id}", scene.ApplicationBody(raters), submitted.ETag);
        var second = await candidate.Client.PostAsync(Applications, scene.ApplicationBody(raters));
        var withdrawn = (await candidate.Client.PostAsync($"{Applications}/{submitted.Id}/withdraw", ifMatch: submitted.ETag)).Expect(HttpStatusCode.OK);
        var again = await candidate.Client.PostAsync($"{Applications}/{submitted.Id}/withdraw", ifMatch: withdrawn.ETag);
        var replacement = await candidate.Client.PostAsync(Applications, scene.ApplicationBody(raters));

        edit.Status.Should().Be(HttpStatusCode.Conflict);
        second.Status.Should().Be(HttpStatusCode.Conflict, "one unfinished application a session");
        PromotionScene.StatusOf(withdrawn).Should().Be("Withdrawn");
        again.Status.Should().Be(HttpStatusCode.Conflict);
        replacement.Expect(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Ac37_AnApplicationIsSeenByItsCandidateTheHeadOfTheTargetUnitAndTheHr_AndByNoOneElse()
    {
        var scene = await fixture.SceneAsync();
        var candidate = await scene.NewCandidateAsync();
        var colleague = await scene.NewCandidateAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var submitted = await scene.SubmitAsync(candidate, raters);

        (await scene.GetApplicationAsync(candidate.Client, submitted.Id)).Expect(HttpStatusCode.OK);
        (await scene.GetApplicationAsync(scene.Org.TeamHead.Client, submitted.Id)).Expect(HttpStatusCode.OK);
        (await scene.GetApplicationAsync(scene.Org.Hr.Client, submitted.Id)).Expect(HttpStatusCode.OK);
        (await scene.GetApplicationAsync(scene.Org.Admin, submitted.Id)).Expect(HttpStatusCode.OK);
        (await scene.GetApplicationAsync(colleague.Client, submitted.Id)).Status.Should().Be(HttpStatusCode.NotFound);
        (await scene.GetApplicationAsync(scene.Org.SiblingHead.Client, submitted.Id)).Status.Should().Be(HttpStatusCode.NotFound);
        (await scene.GetApplicationAsync(scene.Org.Plain.Client, submitted.Id)).Status.Should().Be(HttpStatusCode.NotFound);
        var mine = (await candidate.Client.GetAsync($"{Applications}/mine")).Expect(HttpStatusCode.OK).Json!.AsArray();
        mine.Should().ContainSingle().Which!["id"]!.GetValue<Guid>().Should().Be(submitted.Id);
        (await colleague.Client.GetAsync($"{Applications}/mine")).Expect(HttpStatusCode.OK).Json!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Ac82_AnExcludedEmployee_CannotBeNamedARater_ButOneAlreadyListedStays()
    {
        var scene = await fixture.SceneAsync();
        var candidate = await scene.NewCandidateAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var banned = (await scene.NewRatersAsync(1, 0))[0];
        var draft = (await candidate.Client.PostAsync(Applications, scene.ApplicationBody(raters))).Expect(HttpStatusCode.Created);

        (await scene.Org.Admin.PutAsync($"/api/v1/promotion/settings/rater-exclusions/employees/{banned.Employee}", null)).Expect(HttpStatusCode.NoContent);
        var refused = await candidate.Client.PutAsync($"{Applications}/{draft.Id}", scene.ApplicationBody([.. raters, banned]), draft.ETag);
        (await scene.Org.Admin.PutAsync($"/api/v1/promotion/settings/rater-exclusions/employees/{raters[0].Employee}", null)).Expect(HttpStatusCode.NoContent);
        var kept = await candidate.Client.PutAsync($"{Applications}/{draft.Id}", scene.ApplicationBody(raters), draft.ETag);

        refused.Status.Should().Be(HttpStatusCode.BadRequest);
        refused.FieldErrors("raters").Should().ContainSingle().Which.Should().Contain("запрещено");
        kept.Expect(HttpStatusCode.OK).Json!["raters"]!.AsArray().Single(rater => rater!["employeeId"]!.GetValue<Guid>() == raters[0].Employee)!["isExcluded"]!
            .GetValue<bool>().Should().BeTrue("the rater listed before the exclusion stays, marked");
    }

    [Fact]
    public async Task Ac60_Ac16_TheResponsibleHrEntersTheDate_AndSeesAWarningWhenTheIntervalHasNotPassed()
    {
        var scene = await fixture.SceneAsync();
        var candidate = await scene.NewCandidateAsync();
        var raters = await scene.NewRatersAsync(3, 2);
        var submitted = await scene.SubmitAsync(candidate, raters);
        var formal = $"{Applications}/{submitted.Id}/formal";

        var recent = (await scene.Org.Hr.Client.PutAsync(formal, new { hiredOrPromotedOn = PromotionScene.Today, ignoreInterval = false }, submitted.ETag)).Expect(HttpStatusCode.OK);
        var ignored = (await scene.Org.Hr.Client.PutAsync(formal, new { hiredOrPromotedOn = PromotionScene.Today, ignoreInterval = true }, recent.ETag)).Expect(HttpStatusCode.OK);
        var old = (await scene.Org.Hr.Client.PutAsync(formal, new { hiredOrPromotedOn = new DateOnly(2020, 1, 1), ignoreInterval = false }, ignored.ETag)).Expect(HttpStatusCode.OK);
        var byCandidate = await candidate.Client.PutAsync(formal, new { hiredOrPromotedOn = new DateOnly(2020, 1, 1), ignoreInterval = true }, old.ETag);
        var byOtherHr = await scene.Org.SiblingHr.Client.PutAsync(formal, new { hiredOrPromotedOn = new DateOnly(2020, 1, 1), ignoreInterval = true }, old.ETag);

        recent.Json!["warnings"]!.AsArray().Select(warning => warning!.GetValue<string>()).Should().Equal("IntervalNotPassed");
        recent.Json["formalCheck"]!["isPassed"]!.GetValue<bool>().Should().BeFalse();
        ignored.Json!["formalCheck"]!["isPassed"]!.GetValue<bool>().Should().BeTrue("the flag lifts the interval, and the warning stays");
        ignored.Json["formal"]!["ignoreInterval"]!.GetValue<bool>().Should().BeTrue();
        old.Json!["warnings"]!.AsArray().Should().BeEmpty();
        byCandidate.Status.Should().Be(HttpStatusCode.Forbidden);
        byOtherHr.Status.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<ApiResponse> RefusedSubmitAsync(Member candidate, PromotionScene scene, Member[] raters)
    {
        var draft = (await candidate.Client.PostAsync(Applications, scene.ApplicationBody(raters))).Expect(HttpStatusCode.Created);
        var refused = await candidate.Client.PostAsync($"{Applications}/{draft.Id}/submit", ifMatch: draft.ETag);
        (await candidate.Client.DeleteAsync($"{Applications}/{draft.Id}", draft.ETag)).Expect(HttpStatusCode.NoContent);
        return refused.Status == HttpStatusCode.BadRequest ? refused : throw new Xunit.Sdk.XunitException($"Expected the submission to be refused but got {refused.Code}: {refused.Body}");
    }
}
