using System.Globalization;
using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// What the promotion waves expect of the signed-in user, by what they are to the waves at the moment of asking.
/// What is due follows from the stage the dates and the clock put each wave in, the status of the applications and the flags the stages set,
/// never from how far the stage actions have been carried out. Drafts are the candidate's own business and never appear. Reads only the waves
/// that are running, in a fixed number of queries whatever the size of the organization.
/// </summary>
internal sealed class PromotionDeskSource(
    ICurrentActor actor,
    AppDbContext context,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ICareerModel career,
    CommissionReader commissions,
    ApplicationReader reader,
    PromotionCalendar calendar) : IDeskSource
{
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly int[] FilingStages = [PromotionStage.Submission, PromotionStage.FormalCheck];
    private static readonly int[] ReviewStages = [PromotionStage.Artifacts, PromotionStage.Review];

    /// <inheritdoc />
    public async Task<DeskReport> FindAsync(CancellationToken cancellationToken)
    {
        DeskReport nothing = new([], []);
        if (actor.UserId is not { } userId || await users.FindAsync(userId, cancellationToken) is not { IsActive: true, EmployeeId: { } employeeId })
        {
            return nothing;
        }

        var waves = await RunningWavesAsync(cancellationToken);
        if (waves.Count == 0 || await employees.FindAsync(employeeId, cancellationToken) is not { IsActive: true })
        {
            return nothing;
        }

        var dueApplications = new List<Due>();
        var findings = new List<DeskFinding>();
        findings.AddRange(await FormalCheckFindingsAsync(waves, userId, cancellationToken));
        dueApplications.AddRange(await RatingDueAsync(waves, employeeId, cancellationToken));
        dueApplications.AddRange(await RatersReviewDueAsync(waves, employeeId, cancellationToken));
        dueApplications.AddRange(await DecisionsDueAsync(waves, employeeId, cancellationToken));

        if (dueApplications.Count > 0)
        {
            var names = await employees.FindManyAsync([.. dueApplications.Select(due => due.CandidateId).Distinct()], cancellationToken);
            findings.AddRange(dueApplications.Select(due => new DeskFinding(
                due.Kind,
                new DeskObject(DeskObjectType.PromotionApplication, due.ApplicationId, names[due.CandidateId].FullName),
                IsAction: true,
                Details: due.Details)));
        }

        findings.AddRange(await CandidateFindingsAsync(waves, employeeId, cancellationToken));
        return new DeskReport(findings, []);
    }

    /// <summary>
    /// The waves that are running, by identifier, each with the stage it is in.
    /// </summary>
    private async Task<Dictionary<Guid, (PromotionWave Wave, int Stage)>> RunningWavesAsync(CancellationToken cancellationToken) =>
        (await context.Set<PromotionWave>().AsNoTracking().Where(wave => wave.ProcessedStage < PromotionWave.StageCount).ToListAsync(cancellationToken))
            .Select(wave => (Wave: wave, Stage: calendar.StageOf(wave)))
            .Where(pair => pair.Stage is >= PromotionStage.Submission and <= PromotionStage.Feedback)
            .ToDictionary(pair => pair.Wave.Id);

    private static Guid[] WaveIdsAt(Dictionary<Guid, (PromotionWave Wave, int Stage)> waves, params int[] stages) =>
        [.. waves.Where(pair => stages.Contains(pair.Value.Stage)).Select(pair => pair.Key)];

    private static DeskDetails UntilEndOf(Dictionary<Guid, (PromotionWave Wave, int Stage)> waves, Guid waveId, int deadlineStage) =>
        new(Until: waves[waveId].Wave.EndsOn[deadlineStage - 1]);

    private static DeskObject ObjectOf(PromotionWave wave) =>
        new(DeskObjectType.PromotionWave, wave.Id, wave.StartsOn.ToString(DateFormat, CultureInfo.InvariantCulture));

    /// <summary>
    /// For a responsible HR, the waves in the submission and formal check stages with filed applications of their units that have no date of hiring or
    /// of the last promotion yet; the dates may be entered from the moment an application is filed.
    /// </summary>
    private async Task<IEnumerable<DeskFinding>> FormalCheckFindingsAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var checking = WaveIdsAt(waves, FilingStages);
        if (checking.Length == 0 || !actor.IsHr())
        {
            return [];
        }

        var unitIds = (await career.ResponsibleUnitsAsync(userId, cancellationToken)).Select(unit => unit.Id).ToHashSet();
        if (unitIds.Count == 0)
        {
            return [];
        }

        var counts = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => checking.Contains(application.WaveId) && application.Status == ApplicationStatus.Submitted && application.HiredOrPromotedOn == null)
            .GroupBy(application => new { application.WaveId, application.OrgUnitId })
            .Select(group => new { group.Key.WaveId, group.Key.OrgUnitId, Count = group.Count() })
            .ToListAsync(cancellationToken);
        return counts
            .Where(row => unitIds.Contains(row.OrgUnitId))
            .GroupBy(row => row.WaveId)
            .Select(group => new DeskFinding(
                DeskKind.PromotionFormalCheckPending,
                ObjectOf(waves[group.Key].Wave),
                IsAction: true,
                Details: UntilEndOf(waves, group.Key, PromotionStage.FormalCheck) with { ApplicationCount = group.Sum(row => row.Count) }));
    }

    /// <summary>
    /// The admitted applications of the rating stage the employee was asked to rate and has not answered.
    /// </summary>
    private async Task<IEnumerable<Due>> RatingDueAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var rating = WaveIdsAt(waves, PromotionStage.Rating);
        if (rating.Length == 0)
        {
            return [];
        }

        var due = await (
            from rater in context.Set<PromotionRater>().AsNoTracking()
            join application in context.Set<PromotionApplication>().AsNoTracking() on rater.ApplicationId equals application.Id
            where rater.EmployeeId == employeeId
                && rating.Contains(application.WaveId)
                && application.Status == ApplicationStatus.Admitted
                && !context.Set<PromotionRaterScore>().Any(answer => answer.ApplicationId == application.Id && answer.EmployeeId == employeeId)
            select new { application.Id, application.WaveId, application.EmployeeId })
            .ToListAsync(cancellationToken);
        return due.Select(row => new Due(DeskKind.PromotionRatingRequested, row.Id, row.EmployeeId, UntilEndOf(waves, row.WaveId, PromotionStage.Rating)));
    }

    /// <summary>
    /// The filed applications aimed at a unit the employee heads, while the head may still correct the list of raters and confirm the addresses of those
    /// without an account; the item says when some address is not confirmed.
    /// </summary>
    private async Task<IEnumerable<Due>> RatersReviewDueAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var filing = WaveIdsAt(waves, FilingStages);
        if (filing.Length == 0)
        {
            return [];
        }

        var unitIds = (await employees.FindHeadedUnitsAsync(employeeId, cancellationToken)).Where(unit => unit.IsActive).Select(unit => unit.Id).ToArray();
        if (unitIds.Length == 0)
        {
            return [];
        }

        var due = (await DueInUnitsAsync(DeskKind.PromotionRatersReview, ApplicationStatus.Submitted, filing, unitIds, employeeId, waves, PromotionStage.FormalCheck, cancellationToken)).ToList();
        var dueIds = due.Select(item => item.ApplicationId).ToArray();
        var unconfirmed = (await context.Set<PromotionRater>()
                .AsNoTracking()
                .Where(rater => dueIds.Contains(rater.ApplicationId) && rater.Email != null && rater.EmailConfirmedBy == null)
                .Select(rater => rater.ApplicationId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
        return due.Select(item => unconfirmed.Contains(item.ApplicationId) ? item with { Details = item.Details with { HasUnconfirmedEmails = true } } : item);
    }

    /// <summary>
    /// The applications before the commission still without a decision, from the moment they reach it, aimed at a unit whose commission the employee chairs.
    /// </summary>
    private async Task<IEnumerable<Due>> DecisionsDueAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var review = WaveIdsAt(waves, ReviewStages);
        if (review.Length == 0)
        {
            return [];
        }

        var chaired = await context.Set<PromotionCommission>()
            .AsNoTracking()
            .Where(commission => commission.ChairEmployeeId == employeeId)
            .Select(commission => commission.OrgUnitId)
            .ToListAsync(cancellationToken);
        if (chaired.Count == 0)
        {
            return [];
        }

        var unitIds = (await commissions.ReadAsync(chaired, cancellationToken)).Values
            .Where(roster => roster.ChairEmployeeId == employeeId)
            .Select(roster => roster.UnitId)
            .ToArray();
        return unitIds.Length == 0
            ? []
            : await DueInUnitsAsync(DeskKind.PromotionDecisionsPending, ApplicationStatus.UnderReview, review, unitIds, employeeId, waves, PromotionStage.Review, cancellationToken);
    }

    private async Task<IEnumerable<Due>> DueInUnitsAsync(
        DeskKind kind,
        ApplicationStatus status,
        Guid[] waveIds,
        Guid[] unitIds,
        Guid employeeId,
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        int deadlineStage,
        CancellationToken cancellationToken)
    {
        var due = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => waveIds.Contains(application.WaveId)
                && application.Status == status
                && unitIds.Contains(application.OrgUnitId)
                && application.EmployeeId != employeeId)
            .Select(application => new { application.Id, application.WaveId, application.EmployeeId })
            .ToListAsync(cancellationToken);
        return due.Select(row => new Due(kind, row.Id, row.EmployeeId, UntilEndOf(waves, row.WaveId, deadlineStage)));
    }

    /// <summary>
    /// The employee's own applications that wait for something from them: mandatory artifacts in the artifact stage, the mark of received feedback
    /// from the publication of the results. Named by what the application aims at, since the employee knows whose it is.
    /// </summary>
    private async Task<IEnumerable<DeskFinding>> CandidateFindingsAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var waveIds = WaveIdsAt(waves, PromotionStage.Artifacts, PromotionStage.Feedback);
        if (waveIds.Length == 0)
        {
            return [];
        }

        var own = (await context.Set<PromotionApplication>()
                .AsNoTracking()
                .Where(application => application.EmployeeId == employeeId && waveIds.Contains(application.WaveId))
                .ToListAsync(cancellationToken))
            .Where(application => IsWaitingForCandidate(application, waves[application.WaveId].Stage))
            .ToList();
        if (own.Count == 0)
        {
            return [];
        }

        var targets = await reader.TargetsAsync(own, cancellationToken);
        var ownIds = own.Select(application => application.Id).ToArray();
        var filed = (await context.Set<PromotionArtifact>()
                .AsNoTracking()
                .Where(artifact => ownIds.Contains(artifact.ApplicationId))
                .Select(artifact => new { artifact.ApplicationId, artifact.CompetencyId })
                .ToListAsync(cancellationToken))
            .Select(artifact => (artifact.ApplicationId, artifact.CompetencyId))
            .ToHashSet();
        List<DeskFinding> findings = [];
        foreach (var application in own)
        {
            var facts = targets[application.Id];
            var named = new DeskObject(
                DeskObjectType.PromotionApplication,
                application.Id,
                $"{facts.Target.TrackName} — {facts.Target.LevelName}",
                [facts.Target.UnitName]);
            if (waves[application.WaveId].Stage == PromotionStage.Feedback)
            {
                findings.Add(new DeskFinding(DeskKind.PromotionFeedbackPending, named, IsAction: true, Details: UntilEndOf(waves, application.WaveId, PromotionStage.Feedback)));
                continue;
            }

            var mandatory = await reader.MandatoryArtifactIdsAsync(application, facts.Version, cancellationToken);
            if (mandatory.Any(competencyId => !filed.Contains((application.Id, competencyId))))
            {
                findings.Add(new DeskFinding(DeskKind.PromotionArtifactsRequired, named, IsAction: true, Details: UntilEndOf(waves, application.WaveId, PromotionStage.Artifacts)));
            }
        }

        return findings;
    }

    private sealed record Due(DeskKind Kind, Guid ApplicationId, Guid CandidateId, DeskDetails Details);

    private static bool IsWaitingForCandidate(PromotionApplication application, int stage) => stage switch
    {
        PromotionStage.Artifacts => application.Status == ApplicationStatus.Admitted && application.MeetsAfterRating == false,
        PromotionStage.Feedback => application.Status is ApplicationStatus.Decided or ApplicationStatus.NoDecision && application.FeedbackReceivedAt is null,
        _ => false,
    };
}
