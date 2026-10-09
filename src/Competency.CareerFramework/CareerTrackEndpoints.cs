using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// The API of career tracks. Track names are unique within their direction.
/// </summary>
internal static class CareerTrackEndpoints
{
    private const string Route = "/api/v1/career/tracks";
    private const string DirectionField = "directionId";

    private static readonly CatalogKind<CareerTrack, CareerTrackResponse> Kind = new(
        "CareerTrack",
        CareerTrackResponse.From,
        track => other => other.DirectionId == track.DirectionId);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListCareerTracks");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateCareerTrack").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapEntryEndpoints(Kind, mapRename: true);
    }

    private static Task<Results<Ok<PageResponse<CareerTrackResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        [FromQuery(Name = DirectionField)] Guid? directionId,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken) =>
        CatalogEndpoints.ListAsync(
            context.Set<CareerTrack>().WhereIf(directionId is not null, track => track.DirectionId == directionId),
            query,
            Kind,
            store,
            cancellationToken);

    private static async Task<Results<Created<CareerTrackResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        NewCareerTrackRequest request,
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

        if (!await store.IsUsableAsync<CareerDirection>(request.DirectionId, request.OwnerOrgUnitId, cancellationToken))
        {
            return Rejection.Invalid(DirectionField, CatalogEndpoints.UnavailableMessage);
        }

        var track = new CareerTrack
        {
            Name = request.TrimmedName(),
            OwnerOrgUnitId = request.OwnerOrgUnitId,
            Status = status,
            AuthorId = viewer.UserId,
            DirectionId = request.DirectionId,
        };
        if (await CatalogEndpoints.NameProblemAsync(track, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(track);
        var created = await CatalogEndpoints.CommitAsync(track, Kind, store, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{track.Id}", created);
    }
}
