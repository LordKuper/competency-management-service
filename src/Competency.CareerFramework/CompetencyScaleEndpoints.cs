using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// The API of competency scales. Scale names are unique across the catalogue.
/// </summary>
internal static class CompetencyScaleEndpoints
{
    private const string Route = "/api/v1/career/scales";

    private static readonly CatalogKind<CompetencyScale, CompetencyScaleResponse> Kind = new("CompetencyScale", CompetencyScaleResponse.From);

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
        group.MapEntryEndpoints(Kind);
    }

    private static Task<Results<Ok<PageResponse<CompetencyScaleResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken) =>
        CatalogEndpoints.ListAsync(context.Set<CompetencyScale>(), query, Kind, store, cancellationToken);

    private static async Task<Results<Created<CompetencyScaleResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        NewCompetencyScaleRequest request,
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
        if (viewer.StatusOfNew(request.OwnerOrgUnitId) is not { } status)
        {
            return Rejection.Forbidden();
        }

        var scale = new CompetencyScale
        {
            Name = request.TrimmedName(),
            Points = request.StoredPoints(),
            OwnerOrgUnitId = request.OwnerOrgUnitId,
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

        // TODO(sprint-003-positions-career-competency): reject with 409 a change of a scale that a published matrix version uses, once matrix versions exist (Task 7)
        ifMatch.ApplyTo(context, scale);
        scale.Name = request.TrimmedName();
        scale.Points = request.StoredPoints();
        if (await CatalogEndpoints.NameProblemAsync(scale, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(scale, Kind, store, transaction, response, cancellationToken));
    }
}
