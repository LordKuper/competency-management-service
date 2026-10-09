using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of career levels. Level names are unique within their track. The salary range has endpoints of its own, readable and changeable
/// only by the level's keepers, so no other response ever carries it.
/// </summary>
internal static class CareerLevelEndpoints
{
    private const string Route = "/api/v1/career/levels";
    private const string TrackField = "trackId";
    private const string PreviousLevelField = "previousLevelId";
    private const string NextLevelsField = "nextLevelIds";
    private const string SelfReferenceMessage = "Уровень не может ссылаться на себя";

    internal static readonly CatalogKind<CareerLevel, CareerLevelResponse> Kind = new(
        "CareerLevel",
        CareerLevelResponse.From,
        level => other => other.TrackId == level.TrackId);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListCareerLevels");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateCareerLevel").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateCareerLevel").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/{id:guid}/salary", GetSalaryAsync).WithName("GetCareerLevelSalary").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/{id:guid}/salary", UpdateSalaryAsync).WithName("UpdateCareerLevelSalary").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapEntryEndpoints(Kind);
    }

    private static Task<Results<Ok<PageResponse<CareerLevelResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        [FromQuery(Name = TrackField)] Guid? trackId,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken) =>
        CatalogEndpoints.ListAsync(
            context.Set<CareerLevel>().WhereIf(trackId is not null, level => level.TrackId == trackId),
            query,
            Kind,
            store,
            cancellationToken);

    private static async Task<Results<Created<CareerLevelResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        NewCareerLevelRequest request,
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

        if (!await store.IsUsableAsync<CareerTrack>(request.TrackId, request.OwnerOrgUnitId, cancellationToken))
        {
            return Rejection.Invalid(TrackField, CatalogEndpoints.UnavailableMessage);
        }

        if (await ReferenceProblemAsync(request, request.OwnerOrgUnitId, current: null, store, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var level = new CareerLevel
        {
            Name = request.TrimmedName(),
            Code = request.Code.Trim(),
            OwnerOrgUnitId = request.OwnerOrgUnitId,
            Status = status,
            AuthorId = viewer.UserId,
            TrackId = request.TrackId,
        };
        request.ApplyTo(level);
        if (await CatalogEndpoints.NameProblemAsync(level, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(level);
        await context.Entry(level).Reference(added => added.Track).LoadAsync(cancellationToken);
        var created = await CatalogEndpoints.CommitAsync(level, Kind, store, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{level.Id}", created);
    }

    private static async Task<Results<Ok<CareerLevelResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        CareerLevelRequest request,
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
        if (await store.FindVisibleAsync<CareerLevel>(id, viewer, cancellationToken) is not { } level)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanEdit(level))
        {
            return Rejection.Forbidden();
        }

        if (await ReferenceProblemAsync(request, level.OwnerOrgUnitId, level, store, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        ifMatch.ApplyTo(context, level);
        request.ApplyTo(level);
        if (await CatalogEndpoints.NameProblemAsync(level, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(level, Kind, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// A level the user cannot keep has no readable salary range, so it answers 404 like any unreadable resource.
    /// </summary>
    private static async Task<Results<Ok<CareerLevelSalaryResponse>, NotFound>> GetSalaryAsync(
        Guid id,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CareerLevel>(id, viewer, cancellationToken) is not { } level || !viewer.CanManage(level))
        {
            return TypedResults.NotFound();
        }

        response.SetETag(level.Version);
        return TypedResults.Ok(CareerLevelSalaryResponse.From(level));
    }

    private static async Task<Results<Ok<CareerLevelSalaryResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateSalaryAsync(
        Guid id,
        CareerLevelSalaryRequest request,
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
        if (await store.FindVisibleAsync<CareerLevel>(id, viewer, cancellationToken) is not { } level)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanManage(level))
        {
            return Rejection.Forbidden();
        }

        ifMatch.ApplyTo(context, level);
        level.SalaryFrom = request.SalaryFrom;
        level.SalaryTo = request.SalaryTo;
        await store.SaveAsync(level, transaction, cancellationToken);

        response.SetETag(level.Version);
        return TypedResults.Ok(CareerLevelSalaryResponse.From(level));
    }

    /// <summary>
    /// Says why the previous or next levels cannot be used. Only references the change adds are checked, so a level that has since
    /// been deactivated may stay where it was.
    /// </summary>
    private static async Task<ValidationProblem?> ReferenceProblemAsync(
        CareerLevelRequest request,
        Guid? ownerOrgUnitId,
        CareerLevel? current,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        if (request.PreviousLevelId is { } previousId && previousId != current?.PreviousLevelId)
        {
            if (previousId == current?.Id)
            {
                return Rejection.Invalid(PreviousLevelField, SelfReferenceMessage);
            }

            if (!await store.IsUsableAsync<CareerLevel>(previousId, ownerOrgUnitId, cancellationToken))
            {
                return Rejection.Invalid(PreviousLevelField, CatalogEndpoints.UnavailableMessage);
            }
        }

        foreach (var nextId in request.DistinctNextLevelIds().Except(current?.NextLevelIds ?? []))
        {
            if (nextId == current?.Id)
            {
                return Rejection.Invalid(NextLevelsField, SelfReferenceMessage);
            }

            if (!await store.IsUsableAsync<CareerLevel>(nextId, ownerOrgUnitId, cancellationToken))
            {
                return Rejection.Invalid(NextLevelsField, CatalogEndpoints.UnavailableMessage);
            }
        }

        return null;
    }
}
