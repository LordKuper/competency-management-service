using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of one matrix version: reading it, its completeness and its changes against the published version, editing its content,
/// its approval route steps, the preview of its publication and its history. A version the user cannot read answers 404; a step that is not the user's turn answers 403. The steps that may publish the
/// version take the tag of the confirmed publication list in <c>?cascade=</c>.
/// </summary>
internal static class MatrixVersionEndpoints
{
    public const string Route = "/api/v1/career/matrix-versions";
    private const string CascadeParameter = "cascade";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet("/{id:guid}", GetAsync).WithName("GetMatrixVersion").ProducesETag().ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/{id:guid}/content", EditAsync).WithName("UpdateMatrixVersionContent").ProducesTransition();
        group.MapGet("/{id:guid}/completeness", CompletenessAsync).WithName("GetMatrixVersionCompleteness").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet("/{id:guid}/changes", ChangesAsync).WithName("GetMatrixVersionChanges").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet("/{id:guid}/publication", (Guid id, MatrixWorkflow workflow, CancellationToken cancellationToken) =>
                workflow.PreviewAsync(id, cancellationToken))
            .WithName("GetMatrixVersionPublication")
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/{id:guid}/submit", (Guid id, IfMatch ifMatch, [FromQuery(Name = CascadeParameter)] string? cascade, MatrixWorkflow workflow,
                    HttpResponse response, CancellationToken cancellationToken) =>
                workflow.TransitAsync(id, MatrixAction.Submit, ifMatch, comment: null, cascade, response, cancellationToken))
            .WithName("SubmitMatrixVersion")
            .ProducesPublishingTransition();
        group.MapPost("/{id:guid}/approve", (Guid id, IfMatch ifMatch, [FromQuery(Name = CascadeParameter)] string? cascade, MatrixWorkflow workflow,
                    HttpResponse response, CancellationToken cancellationToken) =>
                workflow.TransitAsync(id, MatrixAction.Approve, ifMatch, comment: null, cascade, response, cancellationToken))
            .WithName("ApproveMatrixVersion")
            .ProducesPublishingTransition();
        group.MapPost("/{id:guid}/return", ReturnAsync).WithName("ReturnMatrixVersion").ProducesTransition();
        group.MapPost("/{id:guid}/accept-changes", (Guid id, IfMatch ifMatch, [FromQuery(Name = CascadeParameter)] string? cascade, MatrixWorkflow workflow,
                    HttpResponse response, CancellationToken cancellationToken) =>
                workflow.TransitAsync(id, MatrixAction.AcceptChanges, ifMatch, comment: null, cascade, response, cancellationToken))
            .WithName("AcceptMatrixVersionChanges")
            .ProducesPublishingTransition();
        group.MapPost("/{id:guid}/accept-and-revise", (Guid id, IfMatch ifMatch, MatrixWorkflow workflow, HttpResponse response, CancellationToken cancellationToken) =>
                workflow.TransitAsync(id, MatrixAction.AcceptAndRevise, ifMatch, comment: null, cascade: null, response, cancellationToken))
            .WithName("AcceptAndReviseMatrixVersion")
            .ProducesTransition();
        group.MapPost("/{id:guid}/rollback-changes", (Guid id, IfMatch ifMatch, MatrixWorkflow workflow, HttpResponse response, CancellationToken cancellationToken) =>
                workflow.TransitAsync(id, MatrixAction.RollbackChanges, ifMatch, comment: null, cascade: null, response, cancellationToken))
            .WithName("RollbackMatrixVersionChanges")
            .ProducesTransition();
        group.MapGet("/{id:guid}/history", HistoryAsync).WithName("GetMatrixVersionHistory").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet("/{id:guid}/variants/{number:int}", VariantAsync).WithName("GetMatrixVersionVariant").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet("/{id:guid}/variants/{number:int}/diff", DiffAsync).WithName("GetMatrixVersionVariantDiff").ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static RouteHandlerBuilder ProducesTransition(this RouteHandlerBuilder builder) => builder
        .ProducesETag()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

    /// <summary>
    /// Declares the responses of a step that may publish the version: its 412 may point to the fresh publication preview.
    /// </summary>
    private static RouteHandlerBuilder ProducesPublishingTransition(this RouteHandlerBuilder builder) => builder
        .ProducesTransition()
        .Produces<PublicationChanged>(StatusCodes.Status412PreconditionFailed, CatalogEndpoints.ProblemContentType);

    private static async Task<Results<Ok<MatrixVersionResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        MatrixWorkflow workflow,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var viewer = await workflow.ViewerAsync(cancellationToken);
        if (await context.Set<MatrixVersion>().AsNoTracking().FirstOrDefaultAsync(version => version.Id == id, cancellationToken) is not { } version
            || await workflow.StateAsync(version, viewer, cancellationToken) is not { } state)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(await workflow.DescribeAsync(version, state, response, cancellationToken));
    }

    private static Task<Results<Ok<MatrixVersionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> EditAsync(
        Guid id,
        MatrixContent request,
        [FromQuery(Name = "builderStep")] int? builderStep,
        IfMatch ifMatch,
        MatrixWorkflow workflow,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        workflow.EditAsync(id, request, builderStep, ifMatch, response, cancellationToken);

    /// <summary>
    /// The completeness check the transitions run, without acting on the version.
    /// </summary>
    private static async Task<Results<Ok<MatrixCompletenessResponse>, NotFound>> CompletenessAsync(
        Guid id,
        AppDbContext context,
        MatrixWorkflow workflow,
        MatrixRoute route,
        MatrixCatalog catalog,
        CancellationToken cancellationToken)
    {
        if (await FindReadableAsync(id, context, workflow, route, cancellationToken) is not { } version)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new MatrixCompletenessResponse(await catalog.CompletenessProblemsAsync(version, cancellationToken)));
    }

    /// <summary>
    /// What the version changes against the published version of its matrix, or against an empty document when none is published.
    /// </summary>
    private static async Task<Results<Ok<MatrixDiffResponse>, NotFound>> ChangesAsync(
        Guid id,
        AppDbContext context,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        if (await FindReadableAsync(id, context, workflow, route, cancellationToken) is not { } version)
        {
            return TypedResults.NotFound();
        }

        var published = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.MatrixId == version.MatrixId && candidate.Status == MatrixVersionStatus.Published, cancellationToken);
        var before = published?.Content ?? MatrixContent.Empty;
        return TypedResults.Ok(new MatrixDiffResponse(
            version.Number,
            published?.Number,
            before,
            version.Content,
            MatrixChange.Between(before, version.Content)));
    }

    private static async Task<MatrixVersion?> FindReadableAsync(
        Guid id,
        AppDbContext context,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        var viewer = await workflow.ViewerAsync(cancellationToken);
        return await context.Set<MatrixVersion>().AsNoTracking().FirstOrDefaultAsync(version => version.Id == id, cancellationToken) is { } version
            && await route.CanReadAsync(viewer, version, isHistory: false, cancellationToken)
            ? version
            : null;
    }

    private static async Task<Results<Ok<MatrixVersionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> ReturnAsync(
        Guid id,
        MatrixReturnRequest request,
        IfMatch ifMatch,
        MatrixWorkflow workflow,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        return await workflow.TransitAsync(id, MatrixAction.Return, ifMatch, request.TrimmedComment(), cascade: null, response, cancellationToken);
    }

    /// <summary>
    /// The variants and decisions of a version, for those who may read its history.
    /// </summary>
    private static async Task<Results<Ok<MatrixHistoryResponse>, NotFound>> HistoryAsync(
        Guid id,
        AppDbContext context,
        CatalogStore store,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        if (!await CanReadHistoryAsync(id, context, workflow, route, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var variants = await context.Set<MatrixVariant>()
            .AsNoTracking()
            .Where(variant => variant.VersionId == id)
            .OrderBy(variant => variant.Number)
            .Select(variant => new { variant.Number, variant.Kind, variant.CreatedById, variant.CreatedAt })
            .ToListAsync(cancellationToken);
        var decisions = await context.Set<MatrixDecision>()
            .AsNoTracking()
            .Where(decision => decision.VersionId == id)
            .OrderBy(decision => decision.Sequence)
            .ToListAsync(cancellationToken);
        var names = await store.UserNamesAsync(
            variants.Select(variant => (Guid?)variant.CreatedById).Concat(decisions.Select(decision => decision.ActorId)),
            cancellationToken);

        return TypedResults.Ok(new MatrixHistoryResponse(
            [.. variants.Select(variant => new MatrixVariantSummaryResponse(
                variant.Number, variant.Kind, variant.CreatedById, names.GetValueOrDefault(variant.CreatedById), variant.CreatedAt))],
            [.. decisions.Select(decision => new MatrixDecisionResponse(
                decision.Sequence,
                decision.Kind,
                decision.Step,
                decision.ActorId,
                decision.ActorId is { } actorId ? names.GetValueOrDefault(actorId) : null,
                decision.At,
                decision.Comment,
                decision.VariantNumber))]));
    }

    private static async Task<Results<Ok<MatrixVariantResponse>, NotFound>> VariantAsync(
        Guid id,
        int number,
        AppDbContext context,
        CatalogStore store,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        if (!await CanReadHistoryAsync(id, context, workflow, route, cancellationToken)
            || await FindVariantAsync(id, number, context, cancellationToken) is not { } variant)
        {
            return TypedResults.NotFound();
        }

        var names = await store.UserNamesAsync([variant.CreatedById], cancellationToken);
        return TypedResults.Ok(new MatrixVariantResponse(
            variant.Number, variant.Kind, variant.CreatedById, names.GetValueOrDefault(variant.CreatedById), variant.CreatedAt, variant.Content));
    }

    /// <summary>
    /// What a variant changed against the variant before it.
    /// </summary>
    private static async Task<Results<Ok<MatrixDiffResponse>, NotFound>> DiffAsync(
        Guid id,
        int number,
        AppDbContext context,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        if (!await CanReadHistoryAsync(id, context, workflow, route, cancellationToken)
            || await FindVariantAsync(id, number, context, cancellationToken) is not { } variant)
        {
            return TypedResults.NotFound();
        }

        var previous = await FindVariantAsync(id, number - 1, context, cancellationToken);
        var before = previous?.Content ?? MatrixContent.Empty;
        return TypedResults.Ok(new MatrixDiffResponse(
            variant.Number,
            previous?.Number,
            before,
            variant.Content,
            MatrixChange.Between(before, variant.Content)));
    }

    private static async Task<bool> CanReadHistoryAsync(
        Guid id,
        AppDbContext context,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        var viewer = await workflow.ViewerAsync(cancellationToken);
        return await context.Set<MatrixVersion>().AsNoTracking().FirstOrDefaultAsync(version => version.Id == id, cancellationToken) is { } version
            && await route.CanReadAsync(viewer, version, isHistory: true, cancellationToken);
    }

    private static Task<MatrixVariant?> FindVariantAsync(Guid id, int number, AppDbContext context, CancellationToken cancellationToken) =>
        context.Set<MatrixVariant>().AsNoTracking().FirstOrDefaultAsync(variant => variant.VersionId == id && variant.Number == number, cancellationToken);
}
