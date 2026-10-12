using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of the matrix versions waiting for the signed-in user's approval; anyone with none waiting gets an empty list.
/// </summary>
internal static class MatrixApprovalEndpoints
{
    private const string Route = "/api/v1/career/approvals";

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapCatalogGroup(Route)
        .MapGet(string.Empty, ListAsync)
        .WithName("ListMatrixApprovals");

    /// <summary>
    /// The versions, longest waiting first, optionally of one unit or at one step.
    /// </summary>
    private static async Task<Ok<List<ApprovalResponse>>> ListAsync(
        [FromQuery(Name = "orgUnitId")] Guid? orgUnitId,
        [FromQuery(Name = "step")] ApprovalStep? step,
        AppDbContext context,
        CatalogStore store,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        var viewer = await workflow.ViewerAsync(cancellationToken);
        var awaiting = (await route.AwaitingAsync(viewer, cancellationToken))
            .Where(item => (orgUnitId is null || item.Version.Matrix.OrgUnitId == orgUnitId) && (step is null || item.Turn.Step == step))
            .ToList();
        var versionIds = awaiting.Select(item => item.Version.Id).ToList();
        var submittedAt = await context.Set<MatrixDecision>()
            .AsNoTracking()
            .Where(decision => versionIds.Contains(decision.VersionId) && decision.Kind == MatrixDecisionKind.Submitted)
            .GroupBy(decision => decision.VersionId)
            .Select(group => new { VersionId = group.Key, At = group.Max(decision => decision.At) })
            .ToDictionaryAsync(item => item.VersionId, item => item.At, cancellationToken);
        var trackIds = awaiting.Select(item => item.Version.Matrix.TrackId).Distinct().ToList();
        var trackNames = await context.Set<CareerTrack>()
            .AsNoTracking()
            .Where(track => trackIds.Contains(track.Id))
            .ToDictionaryAsync(track => track.Id, track => track.Name, cancellationToken);
        var unitNames = await store.UnitNamesAsync(awaiting.Select(item => (Guid?)item.Version.Matrix.OrgUnitId), cancellationToken);

        return TypedResults.Ok(awaiting
            .Select(item => new ApprovalResponse(
                item.Version.Id,
                item.Version.MatrixId,
                item.Version.Number,
                item.Version.Matrix.TrackId,
                trackNames.GetValueOrDefault(item.Version.Matrix.TrackId, string.Empty),
                item.Version.Matrix.OrgUnitId,
                unitNames.GetValueOrDefault(item.Version.Matrix.OrgUnitId, string.Empty),
                item.Turn.Step,
                submittedAt.TryGetValue(item.Version.Id, out var at) ? at : null))
            .OrderBy(approval => approval.SubmittedAt)
            .ThenBy(approval => approval.VersionId)
            .ToList());
    }
}
