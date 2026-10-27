using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Writes the content a candidate gives an application, changing only the rows that differ so the journal records what changed.
/// </summary>
internal static class ApplicationContent
{
    /// <summary>
    /// Sets the target, the education, the self-scores and the raters of an application from a checked request.
    /// </summary>
    /// <param name="context">The context tracking the application.</param>
    /// <param name="application">The application, tracked.</param>
    /// <param name="target">The target the request names.</param>
    /// <param name="request">The checked content.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>A task that ends when the changes are tracked; the caller saves them.</returns>
    public static async Task ApplyAsync(
        AppDbContext context,
        PromotionApplication application,
        ResolvedTarget target,
        ApplicationRequest request,
        CancellationToken cancellationToken)
    {
        application.OrgUnitId = target.Matrix.OrgUnitId;
        application.MatrixId = target.Matrix.MatrixId;
        application.VersionId = target.Matrix.VersionId;
        application.LevelId = target.Level.Id;
        application.Education = request.Education;
        await ReplaceSelfScoresAsync(context, application.Id, request.SelfScores, cancellationToken);
        await ReplaceRatersAsync(context, application.Id, request.RaterEmployeeIds, RaterSource.Candidate, cancellationToken);
    }

    /// <summary>
    /// Replaces the raters of an application; those already on the list keep their source.
    /// </summary>
    /// <param name="context">The context tracking the application.</param>
    /// <param name="applicationId">The application.</param>
    /// <param name="employeeIds">The raters the application is to have.</param>
    /// <param name="source">Who puts the new raters on the list.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>A task that ends when the changes are tracked; the caller saves them.</returns>
    public static async Task ReplaceRatersAsync(
        AppDbContext context,
        Guid applicationId,
        IReadOnlyCollection<Guid> employeeIds,
        RaterSource source,
        CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToList();
        var stored = await context.Set<PromotionRater>().Where(rater => rater.ApplicationId == applicationId).ToListAsync(cancellationToken);
        context.RemoveRange(stored.Where(rater => !ids.Contains(rater.EmployeeId)));
        context.AddRange(ids
            .Where(id => stored.All(rater => rater.EmployeeId != id))
            .Select(id => new PromotionRater { ApplicationId = applicationId, EmployeeId = id, Source = source }));
    }

    private static async Task ReplaceSelfScoresAsync(
        AppDbContext context,
        Guid applicationId,
        IReadOnlyCollection<SelfScoreInput> scores,
        CancellationToken cancellationToken)
    {
        var given = scores.ToDictionary(score => score.CompetencyId, score => score.Score);
        var stored = await context.Set<PromotionSelfScore>().Where(score => score.ApplicationId == applicationId).ToListAsync(cancellationToken);
        context.RemoveRange(stored.Where(score => !given.ContainsKey(score.CompetencyId)));
        foreach (var score in stored.Where(score => given.ContainsKey(score.CompetencyId)))
        {
            score.Score = given[score.CompetencyId];
        }

        context.AddRange(given
            .Where(pair => stored.All(score => score.CompetencyId != pair.Key))
            .Select(pair => new PromotionSelfScore { ApplicationId = applicationId, CompetencyId = pair.Key, Score = pair.Value }));
    }
}
