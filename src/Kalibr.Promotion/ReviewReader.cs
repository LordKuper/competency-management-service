using Kalibr.CareerFramework;
using Kalibr.OrgStructure;
using Kalibr.Platform;
using Kalibr.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.Promotion;

/// <summary>
/// Describes applications to the commission: the card of one candidate and the ranking of a unit, which show what the raters, the candidate and the
/// chair gave, and the rating the chair's scores earn. Only the commission and administrators may be given what it describes. Scoped to a request,
/// because it shares the request's database context.
/// </summary>
internal sealed class ReviewReader(
    AppDbContext context,
    IEmployeeDirectory employees,
    ICareerModel career,
    IUserDirectory users,
    PromotionCalendar calendar,
    WaveReader waves,
    ApplicationReader applications)
{
    private const int ResultStage = PromotionStage.Feedback;

    /// <summary>
    /// Describes the card of one application.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="wave">Its wave.</param>
    /// <param name="roles">What the reader is to the application, not <see cref="ApplicationRole.None"/>.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The card.</returns>
    public async Task<ReviewResponse> DescribeAsync(PromotionApplication application, PromotionWave wave, ApplicationRole roles, CancellationToken cancellationToken)
    {
        var facts = (await applications.TargetsAsync([application], cancellationToken))[application.Id];
        var version = facts.Version;
        var candidate = (await employees.FindAsync(application.EmployeeId, cancellationToken))!;
        var selfScores = await context.Set<PromotionSelfScore>().AsNoTracking().Where(score => score.ApplicationId == application.Id).ToDictionaryAsync(score => score.CompetencyId, score => score.Score, cancellationToken);
        var answers = (await context.Set<PromotionRaterScore>().AsNoTracking().Where(answer => answer.ApplicationId == application.Id).ToListAsync(cancellationToken))
            .ToLookup(answer => answer.CompetencyId);
        var artifacts = (await applications.ArtifactsAsync(application.Id, version, cancellationToken)).ToDictionary(artifact => artifact.CompetencyId);
        var mandatory = await applications.MandatoryArtifactIdsAsync(application, version, cancellationToken);
        var finals = await applications.FinalScoresAsync(application.Id, cancellationToken);
        var (history, latest) = await PreviousAsync(application, cancellationToken);
        var averages = CircularRating.Averages(answers.SelectMany(group => group));
        var standing = finals.Count == 0 ? null : career.Score(version, application.LevelId, finals.ToDictionary(pair => pair.Key, pair => (decimal)pair.Value));
        var defaults = CircularRating.DefaultFinalScores(averages);
        var rated = standing ?? career.Score(version, application.LevelId, defaults);
        var raters = await context.Set<PromotionRater>().AsNoTracking().Where(rater => rater.ApplicationId == application.Id).ToListAsync(cancellationToken);
        var raterIds = raters.Select(rater => rater.EmployeeId).ToArray();
        var raterCounts = ApplicationReader.RaterCountsOf(
            raters,
            candidate.OrgUnitId,
            await employees.FindManyAsync(raterIds, cancellationToken),
            await users.FindByEmployeeIdsAsync(raterIds, cancellationToken));
        var required = version.Requirements.Where(requirement => requirement.LevelId == application.LevelId).ToDictionary(requirement => requirement.CompetencyId, requirement => requirement.RequiredScore);
        var decidedBy = application.DecidedBy is { } userId ? (await waves.AccountNamesAsync([userId], cancellationToken)).GetValueOrDefault(userId) : null;

        return new ReviewResponse(
            application.Id,
            wave.Id,
            calendar.StageOf(wave),
            SessionDatesResponse.From(wave, calendar.StageOf(wave)),
            application.Status,
            [.. Enum.GetValues<ApplicationRole>().Where(role => role != ApplicationRole.None && roles.HasFlag(role))],
            application.EmployeeId,
            candidate.FullName,
            facts.Target,
            application.Education,
            await applications.FormalOfAsync(application, cancellationToken),
            application.FormalFailures,
            application.MeetsAfterRating,
            application.ArtifactsMissing,
            [.. version.Competencies.Select(competency => CompetencyOf(
                competency,
                version.Blocks.FirstOrDefault(block => block.Id == competency.BlockId),
                defaults.TryGetValue(competency.Id, out var defaultScore) ? (int)defaultScore : null,
                required.TryGetValue(competency.Id, out var requiredScore) ? requiredScore : null,
                selfScores.TryGetValue(competency.Id, out var selfScore) ? selfScore : null,
                [.. answers[competency.Id]],
                latest,
                artifacts.GetValueOrDefault(competency.Id),
                mandatory.Contains(competency.Id),
                finals.TryGetValue(competency.Id, out var finalScore) ? finalScore : null,
                standing?.Competencies.FirstOrDefault(item => item.CompetencyId == competency.Id)?.IsMet))],
            raterCounts,
            rated.Rating,
            standing is not null,
            facts.Level.MinOverallPercent,
            rated.IsReady,
            application.Decision,
            decidedBy,
            application.DecidedAt,
            history.Count == 0 ? null : ReadinessChangeOf(history[0], career.Score(version, application.LevelId, averages).Rating),
            history);
    }

    /// <summary>
    /// Ranks the candidates a commission reviews in a wave by the rating the chair's scores earn, the highest first and equal ratings by name;
    /// candidates the chair has not scored follow, by name.
    /// </summary>
    /// <param name="waveId">The wave.</param>
    /// <param name="unitId">The unit whose commission reviews the applications.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>One row per application that reached the commission.</returns>
    public async Task<IReadOnlyList<RankingItemResponse>> RankAsync(Guid waveId, Guid unitId, CancellationToken cancellationToken)
    {
        var statuses = PromotionApplication.ReviewedStatuses.ToArray();
        var reviewed = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == waveId && application.OrgUnitId == unitId && statuses.Contains(application.Status))
            .ToListAsync(cancellationToken);
        var facts = await applications.TargetsAsync(reviewed, cancellationToken);
        var people = await employees.FindManyAsync([.. reviewed.Select(application => application.EmployeeId).Distinct()], cancellationToken);
        var ids = reviewed.Select(application => application.Id).ToArray();
        var finals = (await context.Set<PromotionFinalScore>().AsNoTracking().Where(score => ids.Contains(score.ApplicationId)).ToListAsync(cancellationToken))
            .ToLookup(score => score.ApplicationId);
        var answers = (await context.Set<PromotionRaterScore>().AsNoTracking().Where(answer => ids.Contains(answer.ApplicationId)).ToListAsync(cancellationToken))
            .ToLookup(answer => answer.ApplicationId);

        var rows = reviewed
            .Select(application => (
                Application: application,
                Name: people[application.EmployeeId].FullName,
                IsFromFinals: finals[application.Id].Any(),
                Standing: career.Score(
                    facts[application.Id].Version,
                    application.LevelId,
                    finals[application.Id].Any()
                        ? finals[application.Id].ToDictionary(score => score.CompetencyId, score => (decimal)score.Score)
                        : CircularRating.DefaultFinalScores(CircularRating.Averages(answers[application.Id])))))
            .OrderByDescending(row => row.Standing.Rating)
            .ThenBy(row => row.Name, TextCollation.Comparer)
            .ThenBy(row => row.Application.Id);
        return [.. rows.Select((row, index) => new RankingItemResponse(
            index + 1,
            row.Application.Id,
            row.Application.EmployeeId,
            row.Name,
            facts[row.Application.Id].Target,
            row.Application.Status,
            row.Standing.Rating,
            row.IsFromFinals,
            row.Standing.Threshold,
            row.Standing.IsReady,
            row.Application.Decision))];
    }

    private static ReviewReadinessChangeResponse ReadinessChangeOf(ReviewHistoryResponse previous, decimal rating) =>
        new(
            previous.WaveStartsOn,
            previous.Target,
            previous.Rating,
            rating,
            Math.Round(rating, 1, MidpointRounding.AwayFromZero) - Math.Round(previous.Rating, 1, MidpointRounding.AwayFromZero));

    private static ReviewCompetencyResponse CompetencyOf(
        CareerVersionCompetency competency,
        CareerVersionBlock? block,
        int? defaultFinalScore,
        int? requiredScore,
        int? selfScore,
        IReadOnlyCollection<PromotionRaterScore> answers,
        PreviousScores? latest,
        ArtifactResponse? artifact,
        bool isArtifactMandatory,
        int? finalScore,
        bool? isMet)
    {
        var circular = CircularRating.Summarize(competency, answers);
        return new ReviewCompetencyResponse(
            competency.Id,
            competency.Name,
            competency.Description,
            competency.Type,
            block,
            requiredScore,
            competency.ScalePoints,
            selfScore,
            circular,
            ChangeOf(competency.Id, circular.Average, latest),
            artifact,
            isArtifactMandatory,
            defaultFinalScore,
            finalScore,
            isMet);
    }

    private static ReviewCompetencyChangeResponse? ChangeOf(Guid competencyId, decimal? average, PreviousScores? latest)
    {
        if (latest is null)
        {
            return null;
        }

        if (!latest.CompetencyIds.Contains(competencyId))
        {
            return new ReviewCompetencyChangeResponse(IsNew: true, PreviousAverage: null, Delta: null);
        }

        decimal? previous = latest.Averages.TryGetValue(competencyId, out var value) ? value : null;
        return new ReviewCompetencyChangeResponse(
            IsNew: false,
            previous,
            average is { } current && previous is { } before ? Math.Round(current, 1, MidpointRounding.AwayFromZero) - Math.Round(before, 1, MidpointRounding.AwayFromZero) : null);
    }

    private async Task<(IReadOnlyList<ReviewHistoryResponse> History, PreviousScores? Latest)> PreviousAsync(PromotionApplication application, CancellationToken cancellationToken)
    {
        var earlier = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(other => other.EmployeeId == application.EmployeeId && other.Id != application.Id && other.Status == ApplicationStatus.Decided)
            .ToListAsync(cancellationToken);
        var waveIds = earlier.Select(other => other.WaveId).Distinct().ToArray();
        var published = (await context.Set<PromotionWave>().AsNoTracking().Where(wave => waveIds.Contains(wave.Id)).ToListAsync(cancellationToken))
            .Where(wave => calendar.StageOf(wave) >= ResultStage)
            .ToDictionary(wave => wave.Id);
        var shown = earlier
            .Where(other => published.ContainsKey(other.WaveId))
            .OrderByDescending(other => published[other.WaveId].StartsOn)
            .ThenBy(other => other.Id)
            .ToList();
        var facts = await applications.TargetsAsync(shown, cancellationToken);
        var shownIds = shown.Select(other => other.Id).ToArray();
        var answers = (await context.Set<PromotionRaterScore>().AsNoTracking().Where(answer => shownIds.Contains(answer.ApplicationId)).ToListAsync(cancellationToken))
            .ToLookup(answer => answer.ApplicationId);

        var history = shown.Select(other =>
        {
            var version = facts[other.Id].Version;
            var standing = career.Score(version, other.LevelId, CircularRating.Averages(answers[other.Id]));
            return new ReviewHistoryResponse(
                other.WaveId,
                published[other.WaveId].StartsOn,
                facts[other.Id].Target,
                other.Decision!.Value,
                standing.Rating,
                standing.Threshold,
                [.. CriticalGaps.Of(version, standing).Select(gap => new ReviewFailedCompetencyResponse(
                    gap.CompetencyId,
                    version.Competencies.Single(competency => competency.Id == gap.CompetencyId).Name,
                    gap.RequiredScore,
                    gap.Score))]);
        }).ToList();
        PreviousScores? latest = shown.Count == 0
            ? null
            : new PreviousScores(facts[shown[0].Id].Version.Competencies.Select(competency => competency.Id).ToHashSet(), CircularRating.Averages(answers[shown[0].Id]));
        return (history, latest);
    }

    /// <summary>
    /// What the raters gave in the latest earlier decided application, to compare the current one with.
    /// </summary>
    /// <param name="CompetencyIds">The competencies of the version that application was assessed by.</param>
    /// <param name="Averages">The average by competency; a competency no rater observed is absent.</param>
    private sealed record PreviousScores(IReadOnlySet<Guid> CompetencyIds, IReadOnlyDictionary<Guid, decimal> Averages);
}
