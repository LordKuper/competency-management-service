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
    /// The interval is measured to the last day of the check stage.
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
            var failures = FormalCheck.Failures(application, facts[application.Id].Level.RequiredEducation, months, wave.EndsOn[1]);
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
    /// Finds the units whose commission would review admitted applications but has no chair who acts.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The names of the units; empty when every such commission has a chair.</returns>
    public async Task<IReadOnlyList<string>> UnitsWithoutChairAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var unitIds = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && application.Status == ApplicationStatus.Admitted)
            .Select(application => application.OrgUnitId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return [.. (await commissions.ReadAsync(unitIds, cancellationToken)).Values
            .Where(roster => roster.ChairEmployeeId is null)
            .Select(roster => roster.UnitName)
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Hands the admitted applications to the commission, marking those that owe artifacts they did not give.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task OpenReviewAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var admitted = await ApplicationsAsync(wave.Id, ApplicationStatus.Admitted, cancellationToken);
        var facts = await applications.TargetsAsync(admitted, cancellationToken);
        var ids = admitted.Select(application => application.Id).ToArray();
        var given = (await context.Set<PromotionArtifact>()
                .AsNoTracking()
                .Where(artifact => ids.Contains(artifact.ApplicationId))
                .Select(artifact => new { artifact.ApplicationId, artifact.CompetencyId })
                .ToListAsync(cancellationToken))
            .ToLookup(artifact => artifact.ApplicationId, artifact => artifact.CompetencyId);
        foreach (var application in admitted)
        {
            var mandatory = await applications.MandatoryArtifactIdsAsync(application, facts[application.Id].Version, cancellationToken);
            application.ArtifactsMissing = mandatory.Any(competencyId => !given[application.Id].Contains(competencyId));
            application.Status = ApplicationStatus.UnderReview;
        }
    }

    /// <summary>
    /// Closes the commission stage: an application the chair recorded no decision on is left without one.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task CloseReviewAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        foreach (var application in await ApplicationsAsync(wave.Id, ApplicationStatus.UnderReview, cancellationToken))
        {
            application.Status = ApplicationStatus.NoDecision;
        }
    }

    private Task<List<PromotionApplication>> ApplicationsAsync(Guid waveId, ApplicationStatus status, CancellationToken cancellationToken) =>
        context.Set<PromotionApplication>().Where(application => application.WaveId == waveId && application.Status == status).ToListAsync(cancellationToken);
}
