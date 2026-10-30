using Kalibr.CareerFramework;
using Kalibr.OrgStructure;
using Kalibr.Platform;
using Kalibr.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.Promotion;

/// <summary>
/// Describes applications to those who may see them, each reader seeing only what their role needs: the candidate their own application, raters
/// and artifacts, the result only from the stage that publishes it, the responsible HR and the administration the formal data, the head and the
/// administration the raters. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class ApplicationReader(
    AppDbContext context,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ICareerModel career,
    PromotionCalendar calendar,
    WaveReader waves,
    RaterExclusions exclusions)
{
    private const int ResultStage = PromotionStage.Feedback;
    private const ApplicationRole FormalViewers = ApplicationRole.Administrator | ApplicationRole.Author | ApplicationRole.ResponsibleHr;
    private const ApplicationRole RaterViewers = ApplicationRole.Administrator | ApplicationRole.Head | ApplicationRole.ResponsibleHr;

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
        var mandatory = isCandidate ? await MandatoryArtifactIdsAsync(application, facts.Version, cancellationToken) : null;
        var artifacts = isCandidate ? await ArtifactsAsync(application.Id, facts.Version, cancellationToken) : null;
        var (raterRows, raterCounts) = await RatersForAsync(roles, raters, people, people[application.EmployeeId].OrgUnitId, cancellationToken);

        return new ApplicationResponse(
            application.Id,
            wave.Id,
            stage,
            SessionDatesResponse.From(wave, stage),
            ShownStatus(application.Status, roles, stage),
            [.. Enum.GetValues<ApplicationRole>().Where(role => role != ApplicationRole.None && roles.HasFlag(role))],
            application.EmployeeId,
            people[application.EmployeeId].FullName,
            facts.Target,
            application.Education,
            [.. facts.Version.Competencies
                .Join(scores, competency => competency.Id, score => score.CompetencyId, (competency, score) => new SelfScoreResponse(
                    competency.Id,
                    competency.Name,
                    competency.Description,
                    competency.Type,
                    facts.Version.Blocks.FirstOrDefault(block => block.Id == competency.BlockId),
                    facts.Version.Requirements.FirstOrDefault(requirement => requirement.LevelId == application.LevelId && requirement.CompetencyId == competency.Id)?.RequiredScore,
                    competency.ScalePoints,
                    score.Score))],
            isCandidate ? PreliminaryRatingOf(application, facts.Version, scores) : null,
            raterRows,
            raterCounts,
            canSeeFormal ? await FormalOfAsync(application, cancellationToken) : null,
            application.FormalFailures,
            await WarningsAsync(application, facts.Level, wave, canSeeFormal, cancellationToken),
            canSeeFormal ? await FormalCheckAsync(application, facts.Level, wave, cancellationToken) : null,
            artifacts,
            mandatory,
            mandatory is null ? null : [.. mandatory.Where(competencyId => artifacts!.All(artifact => artifact.CompetencyId != competencyId))],
            application.IsSentToReview,
            mandatory is { Count: > 0 },
            isCandidate || canSeeFormal ? application.FeedbackReceivedAt : null,
            isResultShown ? await ResultAsync(application, facts.Version, cancellationToken) : null);
    }

    private PreliminaryRatingResponse PreliminaryRatingOf(PromotionApplication application, CareerVersion version, IEnumerable<PromotionSelfScore> selfScores)
    {
        var standing = career.Score(version, application.LevelId, selfScores.ToDictionary(score => score.CompetencyId, score => (decimal)score.Score));
        return new PreliminaryRatingResponse(
            standing.Rating,
            standing.Threshold,
            standing.IsReady,
            CriticalGaps.Of(version, standing).Count == 0,
            standing.Competencies.Count(competency => competency.Score is null));
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
            .Join(stored, competency => competency.Id, artifact => artifact.CompetencyId, (competency, artifact) => new ArtifactResponse(competency.Id, competency.Name, artifact.Links))];
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
        return MandatoryArtifactIds(application, version, answers);
    }

    /// <summary>
    /// Finds the competencies the candidate must give an artifact for, from answers the caller has already read.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="version">The version the application is assessed by.</param>
    /// <param name="answers">The answers of the raters to the application.</param>
    /// <returns>The competencies; none until the rating stage has been checked and found the candidate short.</returns>
    public IReadOnlyList<Guid> MandatoryArtifactIds(PromotionApplication application, CareerVersion version, IEnumerable<PromotionRaterScore> answers) =>
        application.MeetsAfterRating == false ? CircularRating.UnmetCompetencyIds(career.Score(version, application.LevelId, CircularRating.Averages(answers))) : [];

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
        var roles = applications.ToDictionary(application => application.Id, viewer.RolesOf);
        var withRaters = applications.Where(application => (roles[application.Id] & RaterViewers) != ApplicationRole.None).Select(application => application.Id).ToArray();
        var (raters, people, accounts) = await RaterFactsAsync(withRaters, applications.Select(application => application.EmployeeId), cancellationToken);
        var months = (await context.Set<PromotionSettings>().AsNoTracking().SingleAsync(cancellationToken)).MinIntervalMonths;
        return [.. applications.Select(application => SummaryOf(
            application,
            wavesById[application.WaveId],
            roles[application.Id],
            people[application.EmployeeId],
            facts[application.Id],
            months,
            withRaters.Contains(application.Id) ? RaterCountsOf([.. raters[application.Id]], people[application.EmployeeId].OrgUnitId, people, accounts) : null))];
    }

    /// <summary>
    /// Describes the applications whose raters a head corrects, as the rows of the head's candidate list.
    /// </summary>
    /// <param name="applications">The applications.</param>
    /// <param name="wave">Their wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>One row per application, by candidate name.</returns>
    public async Task<IReadOnlyList<HeadCandidateResponse>> HeadCandidatesAsync(IReadOnlyCollection<PromotionApplication> applications, PromotionWave wave, CancellationToken cancellationToken)
    {
        var facts = await TargetsAsync(applications, cancellationToken);
        var (raters, people, accounts) = await RaterFactsAsync([.. applications.Select(application => application.Id)], applications.Select(application => application.EmployeeId), cancellationToken);
        var stage = calendar.StageOf(wave);
        return [.. applications
            .Select(application => new HeadCandidateResponse(
                application.Id,
                application.EmployeeId,
                people[application.EmployeeId].FullName,
                facts[application.Id].Target,
                application.Status,
                ApplicationStageEndpoints.IsListOpen(stage, application),
                RaterCountsOf([.. raters[application.Id]], people[application.EmployeeId].OrgUnitId, people, accounts)))
            .OrderBy(row => row.CandidateName, TextCollation.Comparer)
            .ThenBy(row => row.ApplicationId)];
    }

    /// <summary>
    /// Counts the raters of an application against what a filed application needs.
    /// </summary>
    /// <param name="raters">The raters listed.</param>
    /// <param name="candidateUnitId">The unit the candidate belongs to.</param>
    /// <param name="people">The employees, raters among them.</param>
    /// <param name="accounts">The accounts of the raters who have one, by employee.</param>
    /// <returns>The counts.</returns>
    public static RaterCountsResponse RaterCountsOf(
        IReadOnlyCollection<PromotionRater> raters,
        Guid candidateUnitId,
        IReadOnlyDictionary<Guid, EmployeeStatus> people,
        IReadOnlyDictionary<Guid, UserStatus> accounts)
    {
        var fromOtherUnits = raters.Count(rater => people.TryGetValue(rater.EmployeeId, out var person) && person.OrgUnitId != candidateUnitId);
        return new RaterCountsResponse(
            raters.Count,
            ApplicationRules.MinRaters,
            fromOtherUnits,
            ApplicationRules.MinRatersOutsideUnit,
            Math.Max(0, ApplicationRules.MinRaters - raters.Count),
            Math.Max(0, ApplicationRules.MinRatersOutsideUnit - fromOtherUnits),
            raters.Count(rater => !accounts.ContainsKey(rater.EmployeeId) && rater.Email is not null && !rater.IsEmailConfirmed));
    }

    private async Task<(ILookup<Guid, PromotionRater> Raters, IReadOnlyDictionary<Guid, EmployeeStatus> People, IReadOnlyDictionary<Guid, UserStatus> Accounts)> RaterFactsAsync(
        IReadOnlyCollection<Guid> applicationIds,
        IEnumerable<Guid> candidateIds,
        CancellationToken cancellationToken)
    {
        var ids = applicationIds.ToArray();
        var raters = (await context.Set<PromotionRater>().AsNoTracking().Where(rater => ids.Contains(rater.ApplicationId)).ToListAsync(cancellationToken))
            .ToLookup(rater => rater.ApplicationId);
        var raterIds = raters.SelectMany(group => group).Select(rater => rater.EmployeeId).Distinct().ToArray();
        var people = await employees.FindManyAsync([.. candidateIds.Concat(raterIds).Distinct()], cancellationToken);
        return (raters, people, await users.FindByEmployeeIdsAsync(raterIds, cancellationToken));
    }

    private ApplicationSummaryResponse SummaryOf(
        PromotionApplication application,
        PromotionWave wave,
        ApplicationRole roles,
        EmployeeStatus candidate,
        TargetFacts facts,
        int minIntervalMonths,
        RaterCountsResponse? raterCounts)
    {
        var stage = calendar.StageOf(wave);
        var canSeeFormal = (roles & FormalViewers) != ApplicationRole.None;
        var isChecked = canSeeFormal && application.Status == ApplicationStatus.Submitted;
        var failures = isChecked ? FormalCheck.Failures(application, facts.Level.RequiredEducation, minIntervalMonths, wave.ResultsPublishedOn) : [];
        return new ApplicationSummaryResponse(
            application.Id,
            wave.Id,
            wave.StartsOn,
            stage,
            ShownStatus(application.Status, roles, stage),
            application.EmployeeId,
            candidate.FullName,
            facts.Target,
            application.CreatedAt,
            isChecked ? new FormalCheckResponse(failures.Count == 0, failures) : null,
            canSeeFormal ? application.FormalFailures : [],
            isChecked && application.HiredOrPromotedOn is null,
            raterCounts,
            ShownDecision(application.Decision, roles, stage));
    }

    private static ApplicationStatus ShownStatus(ApplicationStatus status, ApplicationRole roles, int stage) =>
        roles == ApplicationRole.Candidate && stage < ResultStage && status is ApplicationStatus.Decided or ApplicationStatus.NoDecision
            ? ApplicationStatus.UnderReview
            : status;

    private static PromotionDecision? ShownDecision(PromotionDecision? decision, ApplicationRole roles, int stage) =>
        roles == ApplicationRole.Candidate && stage < ResultStage ? null : decision;

    private async Task<(List<RaterResponse>? Rows, RaterCountsResponse? Counts)> RatersForAsync(
        ApplicationRole roles,
        IReadOnlyCollection<PromotionRater> raters,
        IReadOnlyDictionary<Guid, EmployeeStatus> people,
        Guid candidateUnitId,
        CancellationToken cancellationToken)
    {
        if ((roles & RaterViewers) == ApplicationRole.None && roles != ApplicationRole.Candidate)
        {
            return (null, null);
        }

        var shown = raters.Where(rater => roles != ApplicationRole.Candidate || rater.Source == RaterSource.Candidate).ToList();
        var accounts = await users.FindByEmployeeIdsAsync([.. shown.Select(rater => rater.EmployeeId)], cancellationToken);
        var units = await employees.FindUnitsAsync([.. shown.Select(rater => people[rater.EmployeeId].OrgUnitId).Distinct()], cancellationToken);
        var confirmers = await waves.AccountNamesAsync([.. shown.Select(rater => rater.EmailConfirmedBy).OfType<Guid>().Distinct()], cancellationToken);
        var excluded = await exclusions.AmongAsync(shown.Select(rater => people[rater.EmployeeId]), cancellationToken);
        var rows = shown
            .Select(rater => new RaterResponse(
                rater.EmployeeId,
                people[rater.EmployeeId].FullName,
                units[people[rater.EmployeeId].OrgUnitId].Name,
                people[rater.EmployeeId].OrgUnitId != candidateUnitId,
                rater.Source,
                accounts.ContainsKey(rater.EmployeeId),
                rater.Email,
                rater.IsEmailConfirmed,
                rater.EmailConfirmedBy is { } confirmedBy ? confirmers.GetValueOrDefault(confirmedBy) : null,
                excluded.Contains(rater.EmployeeId)))
            .OrderBy(rater => rater.FullName, TextCollation.Comparer)
            .ThenBy(rater => rater.EmployeeId)
            .ToList();
        return (rows, RaterCountsOf(shown, candidateUnitId, people, accounts));
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
        PromotionWave wave,
        bool canSeeFormal,
        CancellationToken cancellationToken)
    {
        if (application.Status is not (ApplicationStatus.Draft or ApplicationStatus.Submitted))
        {
            return [];
        }

        var months = (await context.Set<PromotionSettings>().AsNoTracking().SingleAsync(cancellationToken)).MinIntervalMonths;
        return [.. FormalCheck.Warnings(application, level.RequiredEducation, months, wave.ResultsPublishedOn)
            .Where(warning => canSeeFormal || warning == FormalFailure.EducationBelowRequired)];
    }

    private async Task<FormalCheckResponse?> FormalCheckAsync(PromotionApplication application, CareerVersionLevel level, PromotionWave wave, CancellationToken cancellationToken)
    {
        if (application.Status != ApplicationStatus.Submitted)
        {
            return null;
        }

        var months = (await context.Set<PromotionSettings>().AsNoTracking().SingleAsync(cancellationToken)).MinIntervalMonths;
        var failures = FormalCheck.Failures(application, level.RequiredEducation, months, wave.ResultsPublishedOn);
        return new FormalCheckResponse(failures.Count == 0, failures);
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
        var unitIds = applications.Select(application => application.OrgUnitId).Distinct().ToArray();
        var matrices = (await context.Set<PromotionWaveMatrix>()
                .AsNoTracking()
                .Where(matrix => waveIds.Contains(matrix.WaveId) && unitIds.Contains(matrix.OrgUnitId) && matrixIds.Contains(matrix.MatrixId))
                .ToListAsync(cancellationToken))
            .ToDictionary(matrix => (matrix.WaveId, matrix.OrgUnitId, matrix.MatrixId));
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
                var matrix = matrices[(application.WaveId, application.OrgUnitId, application.MatrixId)];
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
