using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Describes applications to those who may see them, each reader seeing only what their role needs: the candidate their own application, raters
/// and artifacts, the result only from the stage that publishes it, the responsible HR and the administration the formal data, the head and the
/// administration the raters. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class ApplicationReader(
    AppDbContext context,
    IEmployeeDirectory employees,
    ICareerModel career,
    PromotionCalendar calendar,
    WaveReader waves)
{
    private const int ResultStage = 6;
    private const ApplicationRole FormalViewers = ApplicationRole.Administrator | ApplicationRole.Author | ApplicationRole.ResponsibleHr;
    private const ApplicationRole RaterViewers = ApplicationRole.Administrator | ApplicationRole.Head;

    /// <summary>
    /// Describes one application.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="wave">Its wave.</param>
    /// <param name="roles">What the reader is to the application, not <see cref="ApplicationRole.None"/>.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The application as the reader may see it.</returns>
    public async Task<ApplicationResponse> DescribeAsync(PromotionApplication application, PromotionWave wave, ApplicationRole roles, CancellationToken cancellationToken)
    {
        var facts = (await TargetsAsync([application], cancellationToken))[application.Id];
        var scores = await context.Set<PromotionSelfScore>().AsNoTracking().Where(score => score.ApplicationId == application.Id).ToListAsync(cancellationToken);
        var raters = await context.Set<PromotionRater>().AsNoTracking().Where(rater => rater.ApplicationId == application.Id).ToListAsync(cancellationToken);
        var people = await employees.FindManyAsync([application.EmployeeId, .. raters.Select(rater => rater.EmployeeId)], cancellationToken);
        var stage = calendar.StageOf(wave);
        var canSeeFormal = (roles & FormalViewers) != ApplicationRole.None;
        var isCandidate = roles == ApplicationRole.Candidate;
        var isResultShown = isCandidate && stage >= ResultStage && application.Status is ApplicationStatus.Decided or ApplicationStatus.NoDecision;

        return new ApplicationResponse(
            application.Id,
            wave.Id,
            stage,
            ShownStatus(application.Status, roles, stage),
            [.. Enum.GetValues<ApplicationRole>().Where(role => role != ApplicationRole.None && roles.HasFlag(role))],
            application.EmployeeId,
            people[application.EmployeeId].FullName,
            facts.Target,
            application.Education,
            [.. facts.Version.Competencies
                .Join(scores, competency => competency.Id, score => score.CompetencyId, (competency, score) => new SelfScoreResponse(competency.Id, competency.Name, score.Score))],
            RatersFor(roles, raters, people),
            canSeeFormal ? await FormalOfAsync(application, cancellationToken) : null,
            application.FormalFailures,
            await WarningsAsync(application, facts.Level, canSeeFormal, cancellationToken),
            isCandidate ? await ArtifactsAsync(application.Id, facts.Version, cancellationToken) : null,
            isCandidate ? await MandatoryArtifactIdsAsync(application, facts.Version, cancellationToken) : null,
            isCandidate || canSeeFormal ? application.FeedbackReceivedAt : null,
            isResultShown ? await ResultAsync(application, facts.Version, cancellationToken) : null);
    }

    /// <summary>
    /// Reads the artifacts of an application.
    /// </summary>
    /// <param name="applicationId">The application.</param>
    /// <param name="version">The version the application is assessed by.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The artifacts in the order of the competencies of the version.</returns>
    public async Task<IReadOnlyList<ArtifactResponse>> ArtifactsAsync(Guid applicationId, CareerVersion version, CancellationToken cancellationToken)
    {
        var stored = await context.Set<PromotionArtifact>().AsNoTracking().Where(artifact => artifact.ApplicationId == applicationId).ToListAsync(cancellationToken);
        return [.. version.Competencies
            .Join(stored, competency => competency.Id, artifact => artifact.CompetencyId, (competency, artifact) => new ArtifactResponse(competency.Id, competency.Name, artifact.Text, artifact.Links))];
    }

    /// <summary>
    /// Finds the competencies the candidate must give an artifact for: those the averages of the raters leave unmet, once the rating stage has been checked
    /// and found the candidate short of the requirements; until then none.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="version">The version the application is assessed by.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The competencies.</returns>
    public async Task<IReadOnlyList<Guid>> MandatoryArtifactIdsAsync(PromotionApplication application, CareerVersion version, CancellationToken cancellationToken)
    {
        if (application.MeetsAfterRating != false)
        {
            return [];
        }

        var answers = await context.Set<PromotionRaterScore>().AsNoTracking().Where(answer => answer.ApplicationId == application.Id).ToListAsync(cancellationToken);
        return CircularRating.UnmetCompetencyIds(career.Score(version, application.LevelId, CircularRating.Averages(answers)));
    }

    /// <summary>
    /// Reads the final scores the chair gave.
    /// </summary>
    /// <param name="applicationId">The application.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The score by competency; empty until the chair gives scores.</returns>
    public async Task<Dictionary<Guid, int>> FinalScoresAsync(Guid applicationId, CancellationToken cancellationToken) =>
        await context.Set<PromotionFinalScore>()
            .AsNoTracking()
            .Where(score => score.ApplicationId == applicationId)
            .ToDictionaryAsync(score => score.CompetencyId, score => score.Score, cancellationToken);

    private async Task<ApplicationResultResponse> ResultAsync(PromotionApplication application, CareerVersion version, CancellationToken cancellationToken)
    {
        var finals = await FinalScoresAsync(application.Id, cancellationToken);
        var standing = career.Score(version, application.LevelId, finals.ToDictionary(pair => pair.Key, pair => (decimal)pair.Value));
        return new ApplicationResultResponse(
            application.Decision,
            [.. standing.Competencies.Select(competency => new ResultCompetencyResponse(
                competency.CompetencyId,
                version.Competencies.Single(candidate => candidate.Id == competency.CompetencyId).Name,
                competency.RequiredScore,
                finals.TryGetValue(competency.CompetencyId, out var score) ? score : null,
                competency.IsMet))]);
    }

    /// <summary>
    /// Describes applications as the rows of a list.
    /// </summary>
    /// <param name="applications">The applications.</param>
    /// <param name="wavesById">Their waves.</param>
    /// <param name="viewer">What the reader is to the applications.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>One row per application, in the order given.</returns>
    public async Task<IReadOnlyList<ApplicationSummaryResponse>> SummariesAsync(
        IReadOnlyCollection<PromotionApplication> applications,
        IReadOnlyDictionary<Guid, PromotionWave> wavesById,
        ApplicationViewer viewer,
        CancellationToken cancellationToken)
    {
        var facts = await TargetsAsync(applications, cancellationToken);
        var people = await employees.FindManyAsync([.. applications.Select(application => application.EmployeeId).Distinct()], cancellationToken);
        return [.. applications.Select(application =>
            SummaryOf(application, wavesById[application.WaveId], viewer.RolesOf(application), people[application.EmployeeId].FullName, facts[application.Id].Target))];
    }

    private ApplicationSummaryResponse SummaryOf(PromotionApplication application, PromotionWave wave, ApplicationRole roles, string candidateName, ApplicationTargetResponse target)
    {
        var stage = calendar.StageOf(wave);
        return new ApplicationSummaryResponse(
            application.Id,
            wave.Id,
            wave.StartsOn,
            stage,
            ShownStatus(application.Status, roles, stage),
            application.EmployeeId,
            candidateName,
            target,
            application.CreatedAt,
            ShownDecision(application.Decision, roles, stage));
    }

    private static ApplicationStatus ShownStatus(ApplicationStatus status, ApplicationRole roles, int stage) =>
        roles == ApplicationRole.Candidate && stage < ResultStage && status is ApplicationStatus.Decided or ApplicationStatus.NoDecision
            ? ApplicationStatus.UnderReview
            : status;

    private static PromotionDecision? ShownDecision(PromotionDecision? decision, ApplicationRole roles, int stage) =>
        roles == ApplicationRole.Candidate && stage < ResultStage ? null : decision;

    private static List<RaterResponse>? RatersFor(ApplicationRole roles, IEnumerable<PromotionRater> raters, IReadOnlyDictionary<Guid, EmployeeStatus> people)
    {
        if ((roles & RaterViewers) == ApplicationRole.None && roles != ApplicationRole.Candidate)
        {
            return null;
        }

        return [.. raters
            .Where(rater => roles != ApplicationRole.Candidate || rater.Source == RaterSource.Candidate)
            .Select(rater => new RaterResponse(rater.EmployeeId, people[rater.EmployeeId].FullName, rater.Source))
            .OrderBy(rater => rater.FullName, TextCollation.Comparer)
            .ThenBy(rater => rater.EmployeeId)];
    }

    /// <summary>
    /// Describes what the responsible HR entered for the formal check.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The date, the interval flag and who set it.</returns>
    public async Task<ApplicationFormalResponse> FormalOfAsync(PromotionApplication application, CancellationToken cancellationToken)
    {
        var by = application.IgnoreIntervalBy is { } userId ? (await waves.AccountNamesAsync([userId], cancellationToken)).GetValueOrDefault(userId) : null;
        return new ApplicationFormalResponse(application.HiredOrPromotedOn, application.IgnoreInterval, by, application.IgnoreIntervalAt);
    }

    private async Task<IReadOnlyList<FormalFailure>> WarningsAsync(
        PromotionApplication application,
        CareerVersionLevel level,
        bool canSeeFormal,
        CancellationToken cancellationToken)
    {
        if (application.Status != ApplicationStatus.Submitted)
        {
            return [];
        }

        var months = (await context.Set<PromotionSettings>().AsNoTracking().SingleAsync(cancellationToken)).MinIntervalMonths;
        return [.. FormalCheck.Warnings(application, level.RequiredEducation, months, calendar.Today)
            .Where(warning => canSeeFormal || warning == FormalFailure.EducationBelowRequired)];
    }

    /// <summary>
    /// Finds the target of applications: the unit, the track and the level they aim at, with the version they are assessed by.
    /// </summary>
    /// <param name="applications">The applications.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The facts by application.</returns>
    public async Task<Dictionary<Guid, TargetFacts>> TargetsAsync(IReadOnlyCollection<PromotionApplication> applications, CancellationToken cancellationToken)
    {
        var waveIds = applications.Select(application => application.WaveId).Distinct().ToArray();
        var matrixIds = applications.Select(application => application.MatrixId).Distinct().ToArray();
        var matrices = (await context.Set<PromotionWaveMatrix>()
                .AsNoTracking()
                .Where(matrix => waveIds.Contains(matrix.WaveId) && matrixIds.Contains(matrix.MatrixId))
                .ToListAsync(cancellationToken))
            .ToDictionary(matrix => (matrix.WaveId, matrix.MatrixId));
        var units = await employees.FindUnitsAsync([.. applications.Select(application => application.OrgUnitId).Distinct()], cancellationToken);
        Dictionary<Guid, CareerVersion> versions = [];
        foreach (var versionId in applications.Select(application => application.VersionId).Distinct())
        {
            versions[versionId] = await career.VersionAsync(versionId, cancellationToken)
                ?? throw new InvalidOperationException($"The matrix version {versionId} of an application is not published or archived.");
        }

        return applications.ToDictionary(
            application => application.Id,
            application =>
            {
                var matrix = matrices[(application.WaveId, application.MatrixId)];
                var level = versions[application.VersionId].Levels.Single(candidate => candidate.Id == application.LevelId);
                return new TargetFacts(
                    new ApplicationTargetResponse(
                        application.OrgUnitId,
                        units[application.OrgUnitId].Name,
                        matrix.MatrixId,
                        matrix.TrackId,
                        matrix.TrackName,
                        level.Id,
                        level.Name),
                    versions[application.VersionId],
                    level);
            });
    }
}
