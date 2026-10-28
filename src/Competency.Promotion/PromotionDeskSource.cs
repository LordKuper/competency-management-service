using System.Globalization;
using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// What the promotion waves expect of the signed-in user, or tell them for reference, by what they are to the waves at the moment of asking.
/// What is due follows from the stage the dates and the clock put each wave in, the status of the applications and the flags the stages set,
/// never from how far the stage actions have been carried out. Reads only the waves that are in a stage with something to do, in a fixed
/// number of queries whatever the size of the organization.
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
    private const int SubmissionStage = 1;
    private const int FormalStage = 2;
    private const int RatingStage = 3;
    private const int ArtifactStage = 4;
    private const int ReviewStage = 5;
    private const int FeedbackStage = 7;
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly int[] DeskStages = [SubmissionStage, FormalStage, RatingStage, ArtifactStage, ReviewStage, FeedbackStage];

    /// <inheritdoc />
    public async Task<DeskReport> FindAsync(CancellationToken cancellationToken)
    {
        DeskReport nothing = new([], []);
        if (actor.UserId is not { } userId || await users.FindAsync(userId, cancellationToken) is not { IsActive: true, EmployeeId: { } employeeId })
        {
            return nothing;
        }

        var waves = await WavesInStagesAsync(cancellationToken);
        if (waves.Count == 0 || await employees.FindAsync(employeeId, cancellationToken) is not { IsActive: true })
        {
            return nothing;
        }

        var dueApplications = new List<Due>();
        var findings = new List<DeskFinding>();
        findings.AddRange(await OpenWaveFindingsAsync(waves, employeeId, cancellationToken));
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
    /// The waves that are in a stage with something to do, by identifier, each with the stage it is in.
    /// </summary>
    private async Task<Dictionary<Guid, (PromotionWave Wave, int Stage)>> WavesInStagesAsync(CancellationToken cancellationToken) =>
        (await context.Set<PromotionWave>().AsNoTracking().Where(wave => wave.ProcessedStage < PromotionWave.StageCount).ToListAsync(cancellationToken))
            .Select(wave => (Wave: wave, Stage: calendar.StageOf(wave)))
            .Where(pair => DeskStages.Contains(pair.Stage))
            .ToDictionary(pair => pair.Wave.Id);

    private static Guid[] WaveIdsAt(Dictionary<Guid, (PromotionWave Wave, int Stage)> waves, int stage) =>
        [.. waves.Where(pair => pair.Value.Stage == stage).Select(pair => pair.Key)];

    private static DeskDetails UntilEndOf(Dictionary<Guid, (PromotionWave Wave, int Stage)> waves, Guid waveId) =>
        new(Until: waves[waveId].Wave.EndsOn[waves[waveId].Stage - 1]);

    private static DeskObject ObjectOf(PromotionWave wave) =>
        new(DeskObjectType.PromotionWave, wave.Id, wave.StartsOn.ToString(DateFormat, CultureInfo.InvariantCulture));

    /// <summary>
    /// The waves taking applications that offer a target and in which the employee has no unfinished application; for reference, since the
    /// employee may well not apply.
    /// </summary>
    private async Task<IEnumerable<DeskFinding>> OpenWaveFindingsAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var open = WaveIdsAt(waves, SubmissionStage);
        if (open.Length == 0)
        {
            return [];
        }

        var applied = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.EmployeeId == employeeId && open.Contains(application.WaveId) && PromotionApplication.UnfinishedStatuses.Contains(application.Status))
            .Select(application => application.WaveId)
            .ToListAsync(cancellationToken);
        var withTargets = await context.Set<PromotionWaveMatrix>()
            .AsNoTracking()
            .Where(matrix => open.Contains(matrix.WaveId))
            .Select(matrix => matrix.WaveId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return withTargets
            .Where(waveId => !applied.Contains(waveId))
            .Select(waveId => new DeskFinding(DeskKind.PromotionWaveOpen, ObjectOf(waves[waveId].Wave), IsAction: false, Details: UntilEndOf(waves, waveId)));
    }

    /// <summary>
    /// For a responsible HR, the waves in the formal check stage with filed applications of their units that have no date of hiring or of the
    /// last promotion yet.
    /// </summary>
    private async Task<IEnumerable<DeskFinding>> FormalCheckFindingsAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var checking = WaveIdsAt(waves, FormalStage);
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
                Details: UntilEndOf(waves, group.Key) with { ApplicationCount = group.Sum(row => row.Count) }));
    }

    /// <summary>
    /// The admitted applications of the rating stage the employee was asked to rate and has not answered.
    /// </summary>
    private async Task<IEnumerable<Due>> RatingDueAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var rating = WaveIdsAt(waves, RatingStage);
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
        return due.Select(row => new Due(DeskKind.PromotionRatingRequested, row.Id, row.EmployeeId, UntilEndOf(waves, row.WaveId)));
    }

    /// <summary>
    /// The admitted applications of the rating stage aimed at a unit the employee heads, whose list of raters the head may review.
    /// </summary>
    private async Task<IEnumerable<Due>> RatersReviewDueAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var rating = WaveIdsAt(waves, RatingStage);
        if (rating.Length == 0)
        {
            return [];
        }

        var unitIds = (await employees.FindHeadedUnitsAsync(employeeId, cancellationToken)).Where(unit => unit.IsActive).Select(unit => unit.Id).ToArray();
        return unitIds.Length == 0
            ? []
            : await DueInUnitsAsync(DeskKind.PromotionRatersReview, ApplicationStatus.Admitted, rating, unitIds, employeeId, waves, cancellationToken);
    }

    /// <summary>
    /// The applications of the commission stage still without a decision, aimed at a unit whose commission the employee chairs.
    /// </summary>
    private async Task<IEnumerable<Due>> DecisionsDueAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var review = WaveIdsAt(waves, ReviewStage);
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
            : await DueInUnitsAsync(DeskKind.PromotionDecisionsPending, ApplicationStatus.UnderReview, review, unitIds, employeeId, waves, cancellationToken);
    }

    private async Task<IEnumerable<Due>> DueInUnitsAsync(
        DeskKind kind,
        ApplicationStatus status,
        Guid[] waveIds,
        Guid[] unitIds,
        Guid employeeId,
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
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
        return due.Select(row => new Due(kind, row.Id, row.EmployeeId, UntilEndOf(waves, row.WaveId)));
    }

    /// <summary>
    /// The employee's own applications that wait for something from them: artifacts in the artifact stage, the mark of received feedback in
    /// the feedback stage. Named by what the application aims at, since the employee knows whose it is.
    /// </summary>
    private async Task<IEnumerable<DeskFinding>> CandidateFindingsAsync(
        Dictionary<Guid, (PromotionWave Wave, int Stage)> waves,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var waveIds = new[] { ArtifactStage, FeedbackStage }.SelectMany(stage => WaveIdsAt(waves, stage)).ToArray();
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
            var details = UntilEndOf(waves, application.WaveId);
            if (waves[application.WaveId].Stage == FeedbackStage)
            {
                findings.Add(new DeskFinding(DeskKind.PromotionFeedbackPending, named, IsAction: true, Details: details));
                continue;
            }

            var mandatory = await reader.MandatoryArtifactIdsAsync(application, facts.Version, cancellationToken);
            if (mandatory.Any(competencyId => !filed.Contains((application.Id, competencyId))))
            {
                findings.Add(new DeskFinding(DeskKind.PromotionArtifactsRequired, named, IsAction: true, Details: details));
            }
        }

        return findings;
    }

    private sealed record Due(DeskKind Kind, Guid ApplicationId, Guid CandidateId, DeskDetails Details);

    private static bool IsWaitingForCandidate(PromotionApplication application, int stage) => stage switch
    {
        ArtifactStage => application.Status == ApplicationStatus.Admitted && application.MeetsAfterRating == false,
        FeedbackStage => application.Status is ApplicationStatus.Decided or ApplicationStatus.NoDecision && application.FeedbackReceivedAt is null,
        _ => false,
    };
}
