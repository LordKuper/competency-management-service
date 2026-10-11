using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of career levels, the parts of a track: whoever may edit the track creates and edits its levels, and a level is kept by its
/// track's keepers. Level names are unique within their track. The salary range has endpoints of its own, readable and changeable only by
/// the level's keepers, so no other response ever carries it.
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
        (level, _) => CareerLevelResponse.From(level),
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

    /// <summary>
    /// Adds a level to an active or draft track; the level takes the track's status, so a draft track's levels are approved with it.
    /// </summary>
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
        if (await store.FindVisibleAsync<CareerTrack>(request.TrackId, viewer, cancellationToken) is not { } track
            || track.Status == CatalogStatus.Inactive)
        {
            return Rejection.Invalid(TrackField, CatalogEndpoints.UnavailableMessage);
        }

        if (!viewer.CanEdit(track))
        {
            return Rejection.Forbidden();
        }

        if (await ReferenceProblemAsync(request, track.OwnerOrgUnitId, current: null, context, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var level = new CareerLevel
        {
            Name = request.TrimmedName(),
            Code = request.Code.Trim(),
            Status = track.Status,
            AuthorId = viewer.UserId,
            TrackId = track.Id,
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

        if (await ReferenceProblemAsync(request, level.Track.OwnerOrgUnitId, level, context, cancellationToken) is { } invalid)
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
    /// Says why the previous or next levels cannot be used: a level may refer to active levels and to the draft levels of its unit's tracks.
    /// Only references the change adds are checked, so a level that has since been deactivated may stay where it was.
    /// </summary>
    private static async Task<ValidationProblem?> ReferenceProblemAsync(
        CareerLevelRequest request,
        Guid unitId,
        CareerLevel? current,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (request.PreviousLevelId is { } previousId && previousId != current?.PreviousLevelId)
        {
            if (previousId == current?.Id)
            {
                return Rejection.Invalid(PreviousLevelField, SelfReferenceMessage);
            }

            if (!await IsUsableAsync(previousId, unitId, context, cancellationToken))
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

            if (!await IsUsableAsync(nextId, unitId, context, cancellationToken))
            {
                return Rejection.Invalid(NextLevelsField, CatalogEndpoints.UnavailableMessage);
            }
        }

        return null;
    }

    private static Task<bool> IsUsableAsync(Guid levelId, Guid unitId, AppDbContext context, CancellationToken cancellationToken) =>
        context.Set<CareerLevel>().AnyAsync(
            level => level.Id == levelId
                && (level.Status == CatalogStatus.Active || (level.Status == CatalogStatus.Draft && level.Track.OwnerOrgUnitId == unitId)),
            cancellationToken);
}
