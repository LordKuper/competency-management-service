using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of competency scales. Scale names are unique across the catalogue. A scale a published matrix version has used never changes,
/// so the version keeps meaning what it meant; the catalogue lock, which publication also takes, keeps the check and a publication apart.
/// Exactly one scale is the default; making another active scale the default takes the flag from the previous one.
/// </summary>
internal static class CompetencyScaleEndpoints
{
    private const string Route = "/api/v1/career/scales";
    private const string PublishedMessage = "Используется в опубликованной матрице — изменить нельзя";
    private const string InactiveDefaultMessage = "По умолчанию может быть только действующая шкала";

    internal static readonly CatalogKind<CompetencyScale, CompetencyScaleResponse> Kind = new("CompetencyScale", (scale, _) => CompetencyScaleResponse.From(scale));

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListCompetencyScales");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateCompetencyScale").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateCompetencyScale").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/make-default", MakeDefaultAsync).WithName("MakeCompetencyScaleDefault").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapEntryEndpoints(Kind);
    }

    private static Task<Results<Ok<PageResponse<CompetencyScaleResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken) =>
        CatalogEndpoints.ListAsync(context.Set<CompetencyScale>(), query, Kind, store, cancellationToken);

    private static async Task<Results<Created<CompetencyScaleResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CompetencyScaleRequest request,
        AppDbContext context,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (viewer.StatusOfNew(unitId: null) is not { } status)
        {
            return Rejection.Forbidden();
        }

        var scale = new CompetencyScale
        {
            Name = request.TrimmedName(),
            Points = request.StoredPoints(),
            Status = status,
            AuthorId = viewer.UserId,
        };
        if (await CatalogEndpoints.NameProblemAsync(scale, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(scale);
        var created = await CatalogEndpoints.CommitAsync(scale, Kind, store, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{scale.Id}", created);
    }

    private static async Task<Results<Ok<CompetencyScaleResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        CompetencyScaleRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CompetencyScale>(id, viewer, cancellationToken) is not { } scale)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanEdit(scale))
        {
            return Rejection.Forbidden();
        }

        if (await context.Set<MatrixVersion>().AnyAsync(
                version => version.ScaleId == scale.Id
                    && (version.Status == MatrixVersionStatus.Published || version.Status == MatrixVersionStatus.Archived),
                cancellationToken))
        {
            return Rejection.Conflict(PublishedMessage);
        }

        ifMatch.ApplyTo(context, scale);
        scale.Name = request.TrimmedName();
        scale.Points = request.StoredPoints();
        if (await CatalogEndpoints.NameProblemAsync(scale, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(scale, Kind, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Makes an active scale the default. The previous default loses the flag in a save of its own first, so the index that allows one
    /// default never sees two.
    /// </summary>
    private static async Task<Results<Ok<CompetencyScaleResponse>, NotFound, ProblemHttpResult>> MakeDefaultAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CompetencyScale>(id, viewer, cancellationToken) is not { } scale)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanManage(scale))
        {
            return Rejection.Forbidden();
        }

        if (scale.Status != CatalogStatus.Active)
        {
            return Rejection.Conflict(InactiveDefaultMessage);
        }

        if (await context.Set<CompetencyScale>().FirstOrDefaultAsync(other => other.IsDefault && other.Id != id, cancellationToken) is { } previous)
        {
            previous.IsDefault = false;
            await context.SaveChangesAsync(cancellationToken);
        }

        ifMatch.ApplyTo(context, scale);
        scale.IsDefault = true;
        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(scale, Kind, store, transaction, response, cancellationToken));
    }
}
