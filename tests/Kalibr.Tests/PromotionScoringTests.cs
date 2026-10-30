using AwesomeAssertions;
using Kalibr.CareerFramework;
using Kalibr.OrgStructure;
using Kalibr.Promotion;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The pure rules of a career session over scores and dates: the rating and readiness by the matrix version's formula, the checks of the scores
/// people enter, the circular summary, the formal check and the state an application returns to when a session goes back a stage.
/// </summary>
public sealed class PromotionScoringTests
{
    private static readonly Guid Level = Guid.NewGuid();
    private static readonly Guid Block = Guid.NewGuid();

    private static readonly IReadOnlyList<ScalePoint> FiveGrades = [.. Enumerable.Range(0, 5).Select(score => new ScalePoint(score, $"Балл {score}"))];

    private static CareerVersionCompetency Competency(Guid? block = null, CompetencyType type = CompetencyType.Additional, string name = "Компетенция") =>
        new(Guid.NewGuid(), name, null, block, type, FiveGrades.Max(point => point.Score), FiveGrades);

    private static CareerVersion VersionOf(
        IReadOnlyList<CareerVersionCompetency> competencies,
        decimal? threshold = 50,
        IEnumerable<(Guid? Block, decimal Percent)>? blockWeights = null,
        IEnumerable<(Guid Competency, decimal Percent)>? competencyWeights = null,
        int required = 3) =>
        new(
            [new CareerVersionLevel(Level, "Уровень", 0, threshold, null)],
            competencies,
            [.. competencies.Select(competency => new CareerVersionRequirement(Level, competency.Id, required))],
            [.. (blockWeights ?? [(null, 100m)]).Select(weight => new CareerVersionBlockWeight(Level, weight.Block, weight.Percent))],
            [new CareerVersionBlock(Block, "Блок", 0)],
            [.. (competencyWeights ?? []).Select(weight => new CareerVersionCompetencyWeight(Level, weight.Competency, weight.Percent))]);

    private static CareerScore Score(CareerVersion version, params (CareerVersionCompetency Competency, decimal Score)[] scores) =>
        new CareerModel(null!, null!, null!, null!).Score(version, Level, scores.ToDictionary(pair => pair.Competency.Id, pair => pair.Score));

    [Fact]
    public void Ac29_ACompetencyWithoutAWeight_CountsAsAnEqualPartOfItsGroup()
    {
        var full = Competency();
        var none = Competency();
        var version = VersionOf([full, none]);

        var score = Score(version, (full, 4), (none, 0));

        score.Rating.Should().Be(50m, "one of two competencies reached the top of its scale");
    }

    [Fact]
    public void Ac93_AGroupWhoseEveryCompetencyHasAWeight_IsAveragedByThoseWeights()
    {
        var weighty = Competency();
        var light = Competency();
        var version = VersionOf([weighty, light], competencyWeights: [(weighty.Id, 75m), (light.Id, 25m)]);

        var score = Score(version, (weighty, 4), (light, 0));

        score.Rating.Should().Be(75m);
    }

    [Fact]
    public void Ac93_AGroupWithWeightsForSomeOfItsCompetenciesOnly_IsAveragedPlainly()
    {
        var weighty = Competency();
        var light = Competency();
        var version = VersionOf([weighty, light], competencyWeights: [(weighty.Id, 75m)]);

        var score = Score(version, (weighty, 4), (light, 0));

        score.Rating.Should().Be(50m);
    }

    [Fact]
    public void Ac93_TheWeightsOfABlock_DoNotReachTheCompetenciesOutsideBlocks()
    {
        var inBlock = Competency(Block);
        var other = Competency(Block);
        var outside = Competency();
        var version = VersionOf(
            [inBlock, other, outside],
            blockWeights: [(Block, 60m), (null, 40m)],
            competencyWeights: [(inBlock.Id, 100m), (other.Id, 0m)]);

        var score = Score(version, (inBlock, 4), (other, 0), (outside, 2));

        score.Rating.Should().Be(60m + 20m, "the block counts 60 at full marks by its own weights, the rest 40 at half");
    }

    [Fact]
    public void Ac21_Ac49_ACompetencyNobodyObserved_AddsNothingToTheRating_AndFailsItsRequirement()
    {
        var observed = Competency();
        var unobserved = Competency();
        var version = VersionOf([observed, unobserved], threshold: null);

        var score = new CareerModel(null!, null!, null!, null!).Score(version, Level, new Dictionary<Guid, decimal> { [observed.Id] = 4 });

        score.Competencies.Single(item => item.CompetencyId == unobserved.Id).IsMet.Should().BeFalse();
        score.Rating.Should().Be(50m);
    }

    [Fact]
    public void Ac93_AThresholdIsNotDemandedOfALevelWithoutOne_AndAGapInACriticalCompetencyStillStopsReadiness()
    {
        var critical = Competency(type: CompetencyType.Critical);
        var version = VersionOf([critical], threshold: null);

        Score(version, (critical, 3)).IsReady.Should().BeTrue("with every competency critical only they decide");
        Score(version, (critical, 2)).IsReady.Should().BeFalse();
    }

    [Fact]
    public void Ac29_ReadinessNeedsTheThresholdAsWellAsEveryCriticalCompetency()
    {
        var critical = Competency(type: CompetencyType.Critical);
        var additional = Competency();
        var version = VersionOf([critical, additional], threshold: 70);

        Score(version, (critical, 3), (additional, 4)).IsReady.Should().BeTrue("the rating is 87.5 and the critical competency reaches its minimum");
        Score(version, (critical, 3), (additional, 0)).IsReady.Should().BeFalse("the rating is 37.5, under the threshold");
        Score(version, (critical, 2), (additional, 4)).IsReady.Should().BeFalse("the rating is 75, but a critical competency below its minimum is not made up for");
    }

    [Fact]
    public void Ac29_Ac19_ScoresAreCheckedAgainstTheVersionsCompetenciesAndScales()
    {
        var first = Competency(name: "Первая");
        var second = Competency(name: "Вторая");
        var version = VersionOf([first, second]);

        ScoreRules.Problems(version, [(first.Id, 4), (second.Id, null)], allowNotObserved: true).Should().BeEmpty();
        ScoreRules.Problems(version, [(first.Id, 4), (second.Id, null)], allowNotObserved: false).Should().ContainSingle().Which.Should().Contain("Вторая");
        ScoreRules.Problems(version, [(first.Id, 9), (second.Id, 1)], allowNotObserved: true).Should().ContainSingle().Which.Should().Contain("Первая");
        ScoreRules.Problems(version, [(first.Id, 1)], allowNotObserved: true).Should().ContainSingle().Which.Should().Contain("Вторая");
        ScoreRules.Problems(version, [(first.Id, 1)], allowNotObserved: true, requireAll: false).Should().BeEmpty("a draft may leave competencies out");
        ScoreRules.Problems(version, [(first.Id, 1), (first.Id, 2), (second.Id, 1)], allowNotObserved: true).Should().NotBeEmpty("a competency is scored once");
        ScoreRules.Problems(version, [(first.Id, 1), (second.Id, 1), (Guid.NewGuid(), 1)], allowNotObserved: true).Should().NotBeEmpty("a competency outside the matrix is refused");
    }

    [Fact]
    public void Ac20_TheCircularSummary_AveragesTheScores_CountsEachGrade_AndCountsTheNotObservedApart()
    {
        var competency = Competency();
        PromotionRaterScore Answer(int? score) => new() { ApplicationId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), CompetencyId = competency.Id, Score = score };

        var summary = CircularRating.Summarize(competency, [Answer(4), Answer(3), Answer(null)]);

        summary.Average.Should().Be(3.5m);
        summary.NotObservedCount.Should().Be(1);
        summary.Distribution.Single(count => count.Score == 4).Count.Should().Be(1);
        summary.Distribution.Single(count => count.Score == 0).Count.Should().Be(0);
        CircularRating.Summarize(competency, [Answer(null)]).Average.Should().BeNull();
    }

    [Fact]
    public void Ac49_AveragesLeaveOutACompetencyEveryRaterSkipped_AndTheDefaultFinalScoreRoundsHalfUp()
    {
        var scored = Guid.NewGuid();
        var skipped = Guid.NewGuid();
        PromotionRaterScore Answer(Guid id, int? score) => new() { ApplicationId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), CompetencyId = id, Score = score };

        var averages = CircularRating.Averages([Answer(scored, 4), Answer(scored, 3), Answer(skipped, null)]);

        averages.Keys.Should().Equal(scored);
        CircularRating.DefaultFinalScores(averages)[scored].Should().Be(4m, "3.5 rounds away from zero");
    }

    [Fact]
    public void Ac81_TheIntervalIsCountedToTheDateOfPublication_AndTwelveFullMonthsPass()
    {
        var published = new DateOnly(2026, 12, 1);
        var application = new PromotionApplication { HiredOrPromotedOn = new DateOnly(2025, 12, 1), Education = EducationLevel.Bachelor };
        var late = new PromotionApplication { HiredOrPromotedOn = new DateOnly(2025, 12, 2), Education = EducationLevel.Bachelor };

        FormalCheck.Failures(application, null, 12, published).Should().BeEmpty();
        FormalCheck.Failures(late, null, 12, published).Should().Equal(FormalFailure.IntervalNotPassed);
    }

    [Fact]
    public void Ac16_Ac17_AnApplicationWithoutADateFails_AndTheIgnoreFlagLiftsOnlyTheInterval()
    {
        var published = new DateOnly(2026, 12, 1);
        var undated = new PromotionApplication { Education = EducationLevel.Bachelor };
        var ignored = new PromotionApplication { HiredOrPromotedOn = published, IgnoreInterval = true, Education = EducationLevel.Bachelor };

        FormalCheck.Failures(undated, null, 12, published).Should().Equal(FormalFailure.MissingDate);
        FormalCheck.Failures(ignored, null, 12, published).Should().BeEmpty();
        FormalCheck.Warnings(ignored, null, 12, published).Should().Equal(FormalFailure.IntervalNotPassed);
    }

    [Fact]
    public void Ac15_Ac81_EducationBelowTheRequiredLevelFails_AndNoRequirementPasses()
    {
        var published = new DateOnly(2026, 12, 1);
        var application = new PromotionApplication { HiredOrPromotedOn = new DateOnly(2020, 1, 1), Education = EducationLevel.SecondaryVocational };

        FormalCheck.Failures(application, EducationLevel.Bachelor, 12, published).Should().Equal(FormalFailure.EducationBelowRequired);
        FormalCheck.Failures(application, EducationLevel.SecondaryVocational, 12, published).Should().BeEmpty();
        FormalCheck.Failures(application, null, 12, published).Should().BeEmpty();
    }

    public static TheoryData<int, string, bool?, bool, string> Returns => new()
    {
        { 2, nameof(ApplicationStatus.FormalRejected), null, false, nameof(ApplicationStatus.Submitted) },
        { 2, nameof(ApplicationStatus.Admitted), true, false, nameof(ApplicationStatus.Submitted) },
        { 3, nameof(ApplicationStatus.UnderReview), true, false, nameof(ApplicationStatus.Admitted) },
        { 4, nameof(ApplicationStatus.UnderReview), false, false, nameof(ApplicationStatus.Admitted) },
        { 4, nameof(ApplicationStatus.UnderReview), true, false, nameof(ApplicationStatus.UnderReview) },
        { 4, nameof(ApplicationStatus.UnderReview), false, true, nameof(ApplicationStatus.UnderReview) },
        { 5, nameof(ApplicationStatus.NoDecision), true, false, nameof(ApplicationStatus.UnderReview) },
        { 5, nameof(ApplicationStatus.Decided), true, false, nameof(ApplicationStatus.Decided) },
        { 4, nameof(ApplicationStatus.Withdrawn), null, false, nameof(ApplicationStatus.Withdrawn) },
        { 2, nameof(ApplicationStatus.Draft), null, false, nameof(ApplicationStatus.Draft) },
    };

    [Theory]
    [MemberData(nameof(Returns))]
    public void Ac53_ReturningAWaveToAStage_SettlesOnlyWhatALaterStageDecided_AndKeepsWhatPeopleEntered(
        int stage,
        string beforeName,
        bool? meetsAfterRating,
        bool sentToReview,
        string expectedName)
    {
        var before = Enum.Parse<ApplicationStatus>(beforeName);
        var expected = Enum.Parse<ApplicationStatus>(expectedName);
        var decision = before == ApplicationStatus.Decided ? (PromotionDecision?)PromotionDecision.Meets : null;
        var application = new PromotionApplication
        {
            Status = before,
            MeetsAfterRating = meetsAfterRating,
            IsSentToReview = sentToReview,
            Decision = decision,
            HiredOrPromotedOn = new DateOnly(2025, 1, 1),
            IgnoreInterval = true,
            FormalFailures = before == ApplicationStatus.FormalRejected ? [FormalFailure.MissingDate] : [],
        };

        application.ReturnToStage(stage);

        application.Status.Should().Be(expected);
        application.Decision.Should().Be(decision, "the chair's decision is kept");
        application.HiredOrPromotedOn.Should().Be(new DateOnly(2025, 1, 1));
        application.IgnoreInterval.Should().BeTrue();
        if (stage <= PromotionStage.FormalCheck && before != ApplicationStatus.Draft)
        {
            application.FormalFailures.Should().BeEmpty();
        }

        if (stage <= PromotionStage.Rating && before != ApplicationStatus.Draft)
        {
            application.MeetsAfterRating.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(PromotionStage.Artifacts)]
    [InlineData(PromotionStage.Review)]
    public void Ac66_AnApplicationHeldForWantOfAChairWhenTheCommissionStageEnded_GoesBackToAdmitted_AndIsMarkedNoMore(int stage)
    {
        var held = new PromotionApplication { Status = ApplicationStatus.NoDecision, MeetsAfterRating = true, WasHeldAtClose = true, ReviewLetterPending = true };

        held.ReturnToStage(stage);

        held.Status.Should().Be(ApplicationStatus.Admitted, "it never reached the commission, so it is handed over with the chair check again");
        held.ReviewLetterPending.Should().BeFalse();
        held.WasHeldAtClose.Should().BeFalse();
    }

    [Fact]
    public void Ac53_AnApplicationTheChairAlreadyDecided_IsDecidedAgainAtOnce_WhenItReachesTheCommissionAgain()
    {
        var application = new PromotionApplication { Status = ApplicationStatus.Admitted, Decision = PromotionDecision.Meets };
        var fresh = new PromotionApplication { Status = ApplicationStatus.Admitted };

        application.ReachCommission();
        fresh.ReachCommission();

        application.Status.Should().Be(ApplicationStatus.Decided);
        fresh.Status.Should().Be(ApplicationStatus.UnderReview);
        fresh.ReviewLetterPending.Should().BeTrue("the members are told once, in one letter");
    }
}
