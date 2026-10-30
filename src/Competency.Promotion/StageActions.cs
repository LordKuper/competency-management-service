using Competency.CareerFramework;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// The changes the end of a stage makes to a wave and its applications. Each method changes tracked rows and saves nothing, so the caller
/// commits them with the stage marker in one transaction. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class StageActions(
    AppDbContext context,
    ICareerModel career,
    ApplicationReader applications,
    CommissionReader commissions)
{
    /// <summary>
    /// Binds the published version of every track matrix of the units of the wave; the wave assesses by these versions whatever is published later.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task FixVersionsAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var unitIds = await context.Set<PromotionWaveUnit>().Where(member => member.WaveId == wave.Id).Select(member => member.OrgUnitId).ToListAsync(cancellationToken);
        var matrices = await career.PublishedTrackMatricesAsync(unitIds, cancellationToken);
        context.AddRange(matrices.Select(matrix => new PromotionWaveMatrix
        {
            WaveId = wave.Id,
            MatrixId = matrix.MatrixId,
            OrgUnitId = matrix.UnitId,
            VersionId = matrix.VersionId,
            TrackId = matrix.TrackId,
            TrackName = matrix.TrackName,
        }));
    }

    /// <summary>
    /// Settles the formal check: a filed application that meets every condition is admitted, any other is rejected with the conditions it failed.
    /// The interval is measured to the day the results are published.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task CheckFormalAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var filed = await ApplicationsAsync(wave.Id, ApplicationStatus.Submitted, cancellationToken);
        var facts = await applications.TargetsAsync(filed, cancellationToken);
        var months = (await context.Set<PromotionSettings>().AsNoTracking().SingleAsync(cancellationToken)).MinIntervalMonths;
        foreach (var application in filed)
        {
            var failures = FormalCheck.Failures(application, facts[application.Id].Level.RequiredEducation, months, wave.ResultsPublishedOn);
            application.FormalFailures = [.. failures];
            application.Status = failures.Count == 0 ? ApplicationStatus.Admitted : ApplicationStatus.FormalRejected;
        }
    }

    /// <summary>
    /// Records for each admitted application whether the averages of the raters meet the mandatory requirements of its version.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task CheckRatingAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var admitted = await ApplicationsAsync(wave.Id, ApplicationStatus.Admitted, cancellationToken);
        var facts = await applications.TargetsAsync(admitted, cancellationToken);
        var ids = admitted.Select(application => application.Id).ToArray();
        var answers = (await context.Set<PromotionRaterScore>().AsNoTracking().Where(answer => ids.Contains(answer.ApplicationId)).ToListAsync(cancellationToken))
            .ToLookup(answer => answer.ApplicationId);
        foreach (var application in admitted)
        {
            application.MeetsAfterRating = career.Score(facts[application.Id].Version, application.LevelId, CircularRating.Averages(answers[application.Id])).IsReady;
        }
    }

    /// <summary>
    /// Hands admitted applications to the commission: those that owe no mandatory artifact when the rating stage ends. At the deadline of the artifact
    /// stage the rest follow, marked as missing the artifacts they did not give; those who sent the application themselves are already there. An application
    /// the chair already decided on, before the wave went back to an earlier stage, is decided again at once. An application of a unit whose commission has
    /// no chair who acts is held: it stays admitted, and a later call hands it over once a chair is there. The first time a unit holds applications
    /// in the wave it is marked, so that its responsible people are told once.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="isDeadline">Whether the artifact stage has ended.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The number of applications handed over, and the number held by each unit that was marked by this call.</returns>
    public async Task<(int Handed, IReadOnlyDictionary<Guid, int> Held)> ReachCommissionAsync(PromotionWave wave, bool isDeadline, CancellationToken cancellationToken)
    {
        var admitted = await ApplicationsAsync(wave.Id, ApplicationStatus.Admitted, cancellationToken);
        var facts = await applications.TargetsAsync(admitted, cancellationToken);
        var rosters = await commissions.ReadAsync([.. admitted.Select(application => application.OrgUnitId).Distinct()], cancellationToken);
        var ids = admitted.Select(application => application.Id).ToArray();
        var given = (await context.Set<PromotionArtifact>()
                .AsNoTracking()
                .Where(artifact => ids.Contains(artifact.ApplicationId))
                .Select(artifact => new { artifact.ApplicationId, artifact.CompetencyId })
                .ToListAsync(cancellationToken))
            .ToLookup(artifact => artifact.ApplicationId, artifact => artifact.CompetencyId);
        var answers = (await context.Set<PromotionRaterScore>().AsNoTracking().Where(answer => ids.Contains(answer.ApplicationId)).ToListAsync(cancellationToken))
            .ToLookup(answer => answer.ApplicationId);
        var reached = 0;
        var held = new Dictionary<Guid, int>();
        foreach (var application in admitted)
        {
            if (rosters.GetValueOrDefault(application.OrgUnitId)?.ChairEmployeeId is null)
            {
                held[application.OrgUnitId] = held.GetValueOrDefault(application.OrgUnitId) + 1;
                continue;
            }

            var mandatory = applications.MandatoryArtifactIds(application, facts[application.Id].Version, answers[application.Id]);
            if (mandatory.Count > 0 && !isDeadline)
            {
                continue;
            }

            application.ArtifactsMissing = mandatory.Any(competencyId => !given[application.Id].Contains(competencyId));
            application.ReachCommission();
            reached++;
        }

        return (reached, await MarkNoChairLettersAsync(wave.Id, held, cancellationToken));
    }

    private async Task<IReadOnlyDictionary<Guid, int>> MarkNoChairLettersAsync(Guid waveId, Dictionary<Guid, int> held, CancellationToken cancellationToken)
    {
        var heldIds = held.Keys.ToArray();
        var unmarked = await context.Set<PromotionWaveUnit>()
            .Where(member => member.WaveId == waveId && !member.NoChairLetterSent && heldIds.Contains(member.OrgUnitId))
            .ToListAsync(cancellationToken);
        foreach (var member in unmarked)
        {
            member.NoChairLetterSent = true;
        }

        return unmarked.ToDictionary(member => member.OrgUnitId, member => held[member.OrgUnitId]);
    }

    /// <summary>
    /// Sends an admitted application that owes artifacts to the commission at the candidate's word, whatever artifacts are given, marking it as missing
    /// those it lacks. From then on the artifacts cannot be changed.
    /// </summary>
    /// <param name="application">The application, tracked.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns><see langword="false"/> when the commission of its unit has no chair who acts, and nothing was changed.</returns>
    public async Task<bool> SendToReviewAsync(PromotionApplication application, CancellationToken cancellationToken)
    {
        if ((await commissions.ReadAsync([application.OrgUnitId], cancellationToken)).GetValueOrDefault(application.OrgUnitId)?.ChairEmployeeId is null)
        {
            return false;
        }

        var version = (await career.VersionAsync(application.VersionId, cancellationToken))!;
        var mandatory = await applications.MandatoryArtifactIdsAsync(application, version, cancellationToken);
        var given = await context.Set<PromotionArtifact>()
            .AsNoTracking()
            .Where(artifact => artifact.ApplicationId == application.Id)
            .Select(artifact => artifact.CompetencyId)
            .ToListAsync(cancellationToken);
        application.ArtifactsMissing = mandatory.Any(competencyId => !given.Contains(competencyId));
        application.IsSentToReview = true;
        application.ReachCommission();
        return true;
    }

    /// <summary>
    /// Collects the applications that reached a commission since its members were last told, and marks them told. Saves the tracked changes first,
    /// so that applications handed over by the same step are found.
    /// </summary>
    /// <param name="waveId">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The number of such applications by the unit whose commission reviews them; empty when there are none.</returns>
    public async Task<IReadOnlyDictionary<Guid, int>> TakeArrivalsAsync(Guid waveId, CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        var flagged = await context.Set<PromotionApplication>().Where(application => application.WaveId == waveId && application.ReviewLetterPending).ToListAsync(cancellationToken);
        foreach (var application in flagged)
        {
            application.ReviewLetterPending = false;
        }

        return flagged
            .Where(application => PromotionApplication.ReviewedStatuses.Contains(application.Status))
            .GroupBy(application => application.OrgUnitId)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    /// <summary>
    /// Closes the commission stage: an application the chair recorded no decision on, and one still held for want of a chair, is left without a decision.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task CloseReviewAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var undecided = await context.Set<PromotionApplication>()
            .Where(application => application.WaveId == wave.Id && (application.Status == ApplicationStatus.UnderReview || application.Status == ApplicationStatus.Admitted))
            .ToListAsync(cancellationToken);
        foreach (var application in undecided)
        {
            application.WasHeldAtClose = application.Status == ApplicationStatus.Admitted;
            application.Status = ApplicationStatus.NoDecision;
        }
    }

    private Task<List<PromotionApplication>> ApplicationsAsync(Guid waveId, ApplicationStatus status, CancellationToken cancellationToken) =>
        context.Set<PromotionApplication>().Where(application => application.WaveId == waveId && application.Status == status).ToListAsync(cancellationToken);
}
