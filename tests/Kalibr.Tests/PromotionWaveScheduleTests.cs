using AwesomeAssertions;
using Kalibr.Platform;
using Kalibr.Promotion;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The dates of a career session and what follows from them: which dates are accepted, which stage a session is in at a moment of the
/// organization's day, and which step the scheduler owes. All three run on dates and a clock alone, so they are checked without a host.
/// </summary>
public sealed class PromotionWaveScheduleTests
{
    private static readonly DateOnly Today = new(2026, 11, 2);
    private static readonly DateOnly Announced = Today;
    private static readonly DateOnly FirstDay = Today.AddDays(WaveDates.MinLeadDays);

    private static DateOnly[] Ends(DateOnly start, params int[] lengths)
    {
        var ends = new List<DateOnly>();
        var current = start;
        foreach (var length in lengths)
        {
            current = current.AddDays(length);
            ends.Add(current);
        }

        return [.. ends];
    }

    private static DateOnly[] SixEnds(DateOnly start) => Ends(start, 10, 2, 5, 2, 5, 5);

    private static WaveRequest Request(DateOnly start, DateOnly[]? ends = null) =>
        new() { StartsOn = start, EndsOn = ends ?? SixEnds(start), UnitIds = [Guid.NewGuid()] };

    private static PromotionWave Wave(DateOnly start, DateOnly[]? ends = null) => new() { StartsOn = start, EndsOn = ends ?? SixEnds(start) };

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static PromotionCalendar CalendarAt(DateTimeOffset utc, string zone = "Europe/Moscow") =>
        new(new Clock(utc), Options.Create(new AppOptions { PublicBaseUrl = new Uri("https://calibr.test.local"), TimeZone = zone }));

    private static DateTimeOffset Moscow(DateOnly day, int hour = 12, int minute = 0) =>
        new(day.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.FromHours(3));

    [Fact]
    public void Ac56_AnnouncedWave_StartsNoEarlierThanSevenDaysAfterTheAnnouncement()
    {
        WaveDates.Validate(Request(FirstDay), null, 0, Announced, Today).Should().BeEmpty();

        var tooEarly = WaveDates.Validate(Request(FirstDay.AddDays(-1), SixEnds(FirstDay)), null, 0, Announced, Today);

        tooEarly.Should().ContainKey("startsOn");
    }

    [Fact]
    public void Ac56_Ac57_AnnouncedWave_EndsEveryStageOnOrAfterTheOneBefore_AndNotBeforeToday()
    {
        var ends = SixEnds(FirstDay);
        ends[2] = ends[1].AddDays(-1);

        var errors = WaveDates.Validate(Request(FirstDay, ends), null, 0, Announced, Today);

        errors.Keys.Should().Equal("endsOn[2]");
    }

    [Fact]
    public void Ac56_ASixStageWave_NeedsAnEndDateForEveryStage()
    {
        WaveDates.Validate(Request(FirstDay, SixEnds(FirstDay)[..5]), null, 0, Announced, Today).Should().ContainKey("endsOn");
    }

    [Fact]
    public void Ac57_ARunningWave_KeepsItsStartAndTheDatesOfFinishedStages_ButMayExtendTheCurrentStage()
    {
        var wave = Wave(FirstDay);
        var extended = wave.EndsOn.ToArray();
        extended[2] = extended[2].AddDays(3);
        extended[3] = extended[3].AddDays(3);
        extended[4] = extended[4].AddDays(3);
        extended[5] = extended[5].AddDays(3);
        var onThirdStage = FirstDay.AddDays(14);

        var extension = WaveDates.Validate(Request(FirstDay, extended), wave, 3, Announced, onThirdStage);
        var movedStart = WaveDates.Validate(Request(FirstDay.AddDays(1), wave.EndsOn), wave, 3, Announced, onThirdStage);
        var movedFinished = wave.EndsOn.ToArray();
        movedFinished[0] = movedFinished[0].AddDays(1);
        var finished = WaveDates.Validate(Request(FirstDay, movedFinished), wave, 3, Announced, onThirdStage);

        extension.Should().BeEmpty();
        movedStart.Keys.Should().Equal("startsOn");
        finished.Keys.Should().Equal("endsOn[0]");
    }

    [Fact]
    public void Ac57_ChangingAWave_MayKeepOrMoveADateIntoThePast()
    {
        var wave = Wave(FirstDay);
        var later = FirstDay.AddDays(30);

        WaveDates.Validate(Request(FirstDay, wave.EndsOn), wave, 0, Announced, later).Should().BeEmpty();
    }

    [Fact]
    public void Ac52_Ac57_RollingBack_KeepsTheEndsOfTheStagesBefore_AndNeedsTodayOrLaterFromTheStageItReturnsTo()
    {
        var wave = Wave(FirstDay);
        var today = wave.EndsOn[3].AddDays(1);
        var renewed = Ends(today, 3, 3, 3, 3);
        var request = new RollbackRequest { Stage = 3, EndsOn = [.. wave.EndsOn[..2], .. renewed] };
        var rewrittenHistory = new RollbackRequest { Stage = 3, EndsOn = [wave.EndsOn[0].AddDays(1), .. request.EndsOn[1..]] };
        var inThePast = new RollbackRequest { Stage = 3, EndsOn = [.. wave.EndsOn[..2], today.AddDays(-1), .. renewed[1..]] };

        WaveDates.ValidateRollback(request, wave, today).Should().BeEmpty();
        WaveDates.ValidateRollback(rewrittenHistory, wave, today).Keys.Should().Equal("endsOn[0]");
        WaveDates.ValidateRollback(inThePast, wave, today).Keys.Should().Equal("endsOn[2]");
    }

    [Fact]
    public void Ac55_ADayBelongsToTheStageThatEndsOnIt_AndTheNextStageStartsWithTheNextDay()
    {
        var wave = Wave(FirstDay);
        var firstEnd = wave.EndsOn[0];

        CalendarAt(Moscow(FirstDay, 0, 0).AddMinutes(-1)).StageOf(wave).Should().Be(0);
        CalendarAt(Moscow(FirstDay, 0, 0)).StageOf(wave).Should().Be(1);
        CalendarAt(Moscow(firstEnd, 23, 59)).StageOf(wave).Should().Be(1, "a stage ends with the last moment of its last day");
        CalendarAt(Moscow(firstEnd.AddDays(1), 0, 0)).StageOf(wave).Should().Be(2);
        CalendarAt(Moscow(wave.EndsOn[5].AddDays(1), 0, 0)).StageOf(wave).Should().Be(PromotionCalendar.AfterLastStage);
    }

    [Fact]
    public void Ac55_AStageEndingOnTheDayTheOneBeforeEnds_LastsNoTimeAndIsNeverCurrent()
    {
        var ends = SixEnds(FirstDay);
        ends[1] = ends[0];
        var wave = Wave(FirstDay, ends);

        var stages = Enumerable.Range(0, 40).Select(offset => CalendarAt(Moscow(FirstDay.AddDays(offset))).StageOf(wave)).Distinct();

        stages.Should().NotContain(2);
    }

    [Fact]
    public void Ac55_TheDayIsTheOrganizationsDay_NotUtc()
    {
        var wave = Wave(FirstDay);
        var lateEvening = new DateTimeOffset(wave.EndsOn[0].ToDateTime(new TimeOnly(21, 30)), TimeSpan.Zero);

        CalendarAt(lateEvening).StageOf(wave).Should().Be(2, "21:30 UTC is already the next day in Moscow");
        CalendarAt(lateEvening, "UTC").StageOf(wave).Should().Be(1);
    }

    [Fact]
    public void Ac55_AWaveNotStarted_IsOwedNothing_AndOnceStartedIsOwedItsFirstStage()
    {
        var wave = Wave(FirstDay);

        DueStep.Next(wave, CalendarAt(Moscow(FirstDay.AddDays(-1))), hasArrivals: false).Should().BeNull();
        DueStep.Next(wave, CalendarAt(Moscow(FirstDay)), hasArrivals: false).Should().Be(new DueStep(DueKind.StartFirstStage, PromotionStage.Submission));
    }

    [Fact]
    public void Ac55_AServiceThatWasDown_CatchesUpOneStageEndAtATime_InOrder()
    {
        var wave = Wave(FirstDay);
        wave.VersionsFixed = true;
        var calendar = CalendarAt(Moscow(wave.EndsOn[2].AddDays(1)));
        var steps = new List<DueStep>();

        while (DueStep.Next(wave, calendar, hasArrivals: false) is { } step)
        {
            steps.Add(step);
            wave.ProcessedStage = step.Stage;
        }

        steps.Should().Equal(new DueStep(DueKind.EndStage, 1), new DueStep(DueKind.EndStage, 2), new DueStep(DueKind.EndStage, 3));
    }

    [Fact]
    public void Ac63_Ac80_ApplicationsThatReachedTheCommission_AreOwedOneLetterToTheMembers()
    {
        var wave = Wave(FirstDay);
        wave.VersionsFixed = true;
        wave.ProcessedStage = 2;
        var calendar = CalendarAt(Moscow(wave.EndsOn[1].AddDays(1)));

        DueStep.Next(wave, calendar, hasArrivals: true).Should().Be(new DueStep(DueKind.NotifyCommissions, PromotionStage.Rating));
    }

    [Fact]
    public void Ac66_ApplicationsHeldForWantOfAChair_AreOwedAHandOver_FromTheRatingCheckToTheEndOfTheReviewStage()
    {
        var wave = Wave(FirstDay);
        wave.VersionsFixed = true;
        var ratingRunning = CalendarAt(Moscow(wave.EndsOn[1].AddDays(1)));
        var artifactStage = CalendarAt(Moscow(wave.EndsOn[2].AddDays(1)));
        var reviewStage = CalendarAt(Moscow(wave.EndsOn[3].AddDays(1)));
        var feedbackStage = CalendarAt(Moscow(wave.EndsOn[4].AddDays(1)));

        wave.ProcessedStage = 2;
        DueStep.Next(wave, ratingRunning, hasArrivals: false, hasAdmitted: true).Should().BeNull("nothing is admitted for the commission before the rating check");
        wave.ProcessedStage = 3;
        DueStep.Next(wave, artifactStage, hasArrivals: false, hasAdmitted: true).Should().Be(new DueStep(DueKind.ReachHeld, PromotionStage.Artifacts));
        DueStep.Next(wave, artifactStage, hasArrivals: false, hasAdmitted: false).Should().BeNull();
        DueStep.Next(wave, artifactStage, hasArrivals: true, hasAdmitted: true).Should().Be(new DueStep(DueKind.NotifyCommissions, PromotionStage.Artifacts), "the letters of arrived applications come first");
        wave.ProcessedStage = 4;
        DueStep.Next(wave, reviewStage, hasArrivals: false, hasAdmitted: true).Should().Be(new DueStep(DueKind.ReachHeld, PromotionStage.Review));
        wave.ProcessedStage = 5;
        DueStep.Next(wave, feedbackStage, hasArrivals: false, hasAdmitted: true).Should().BeNull("the commission stage is over, the held ones have no decision");
    }

    [Fact]
    public void Ac63_TheRatersAreRemindedOnce_WhenLessThanTwoDaysOfTheRatingStageRemain()
    {
        var wave = Wave(FirstDay);
        wave.VersionsFixed = true;
        wave.ProcessedStage = 2;
        var ratingEnd = wave.EndsOn[2];
        var early = CalendarAt(Moscow(ratingEnd.AddDays(-4)));
        var close = CalendarAt(Moscow(ratingEnd.AddDays(-1)));

        DueStep.Next(wave, early, hasArrivals: false).Should().BeNull();
        DueStep.Next(wave, close, hasArrivals: false).Should().Be(new DueStep(DueKind.RemindRaters, PromotionStage.Rating));
        wave.RaterReminderSent = true;
        DueStep.Next(wave, close, hasArrivals: false).Should().BeNull();
    }
}
