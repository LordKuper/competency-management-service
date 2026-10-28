using Competency.Platform;
using Competency.UserManagement;
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
    /// <param name="users">The accounts, to tell the raters who have one.</param>
    /// <param name="application">The application, tracked.</param>
    /// <param name="target">The target the request names.</param>
    /// <param name="request">The checked content.</param>
    /// <param name="now">The moment of the edit.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>A task that ends when the changes are tracked; the caller saves them.</returns>
    public static async Task ApplyAsync(
        AppDbContext context,
        IUserDirectory users,
        PromotionApplication application,
        ResolvedTarget target,
        ApplicationRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        application.OrgUnitId = target.Matrix.OrgUnitId;
        application.MatrixId = target.Matrix.MatrixId;
        application.VersionId = target.Matrix.VersionId;
        application.LevelId = target.Level.Id;
        application.Education = request.Education;
        await ReplaceSelfScoresAsync(context, application.Id, request.SelfScores, cancellationToken);
        await ReplaceRatersAsync(context, users, application.Id, new RaterEdit(request.Raters, RaterSource.Candidate, ConfirmedBy: null, now), cancellationToken);
    }

    /// <summary>
    /// Replaces the raters of an application; those already on the list keep their source. A rater who leaves the list takes their answers with them.
    /// A rater who has an account keeps no address; an address that changes is confirmed when the edit comes from the head or the responsible HR, and not
    /// otherwise, while one that stays keeps its confirmation.
    /// </summary>
    /// <param name="context">The context tracking the application.</param>
    /// <param name="users">The accounts, to tell the raters who have one.</param>
    /// <param name="applicationId">The application.</param>
    /// <param name="edit">The raters the application is to have.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>A task that ends when the changes are tracked; the caller saves them.</returns>
    public static async Task ReplaceRatersAsync(AppDbContext context, IUserDirectory users, Guid applicationId, RaterEdit edit, CancellationToken cancellationToken)
    {
        var listed = edit.Raters.GroupBy(rater => rater.EmployeeId).Select(group => group.First()).ToList();
        var ids = listed.Select(rater => rater.EmployeeId).ToList();
        var withAccount = await users.FindByEmployeeIdsAsync(ids, cancellationToken);
        var stored = await context.Set<PromotionRater>().Where(rater => rater.ApplicationId == applicationId).ToListAsync(cancellationToken);
        var removed = stored.Where(rater => !ids.Contains(rater.EmployeeId)).ToList();
        var removedIds = removed.Select(rater => rater.EmployeeId).ToList();
        context.RemoveRange(await context.Set<PromotionRaterScore>()
            .Where(score => score.ApplicationId == applicationId && removedIds.Contains(score.EmployeeId))
            .ToListAsync(cancellationToken));
        context.RemoveRange(removed);
        foreach (var input in listed)
        {
            var rater = stored.FirstOrDefault(item => item.EmployeeId == input.EmployeeId);
            if (rater is null)
            {
                rater = new PromotionRater { ApplicationId = applicationId, EmployeeId = input.EmployeeId, Source = edit.Source };
                context.Add(rater);
            }

            var email = withAccount.ContainsKey(input.EmployeeId) || string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
            if (!string.Equals(rater.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                rater.Email = email;
                rater.EmailConfirmedBy = email is null ? null : edit.ConfirmedBy;
                rater.EmailConfirmedAt = email is null || edit.ConfirmedBy is null ? null : edit.Now;
            }
        }
    }

    /// <summary>
    /// Replaces the artifacts of an application; a competency left out has none afterwards.
    /// </summary>
    /// <param name="context">The context tracking the application.</param>
    /// <param name="applicationId">The application.</param>
    /// <param name="artifacts">The artifacts the application is to have.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>A task that ends when the changes are tracked; the caller saves them.</returns>
    public static async Task ReplaceArtifactsAsync(
        AppDbContext context,
        Guid applicationId,
        IReadOnlyCollection<ArtifactInput> artifacts,
        CancellationToken cancellationToken)
    {
        var given = artifacts.ToDictionary(artifact => artifact.CompetencyId);
        var stored = await context.Set<PromotionArtifact>().Where(artifact => artifact.ApplicationId == applicationId).ToListAsync(cancellationToken);
        context.RemoveRange(stored.Where(artifact => !given.ContainsKey(artifact.CompetencyId)));
        foreach (var artifact in stored.Where(artifact => given.ContainsKey(artifact.CompetencyId)))
        {
            artifact.Text = given[artifact.CompetencyId].Text.Trim();
            artifact.Links = [.. given[artifact.CompetencyId].Links.Select(link => link.Trim())];
        }

        context.AddRange(given.Values
            .Where(artifact => stored.All(item => item.CompetencyId != artifact.CompetencyId))
            .Select(artifact => new PromotionArtifact
            {
                ApplicationId = applicationId,
                CompetencyId = artifact.CompetencyId,
                Text = artifact.Text.Trim(),
                Links = [.. artifact.Links.Select(link => link.Trim())],
            }));
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
