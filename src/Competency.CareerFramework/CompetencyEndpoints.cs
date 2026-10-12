using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of the competency catalogue. Competency names are unique within their direction, and among general competencies.
/// Every change adds a version to the competency's history, which anyone who can read the competency can read. A request that names no
/// scale gets the default scale. A competency no matrix uses can be deleted with its history.
/// </summary>
internal static class CompetencyEndpoints
{
    private const string Route = "/api/v1/career/competencies";
    private const string CategoryField = "categoryId";
    private const string DirectionField = "directionId";
    private const string WithGeneralField = "withGeneral";
    private const string ScaleField = "scaleId";
    private const string TypeField = "type";
    private const string InUseMessage = "Компетенцию используют матрицы — удалить нельзя";

    internal static readonly CatalogKind<CareerCompetency, CompetencyResponse> Kind = new(
        "Competency",
        (competency, _) => CompetencyResponse.From(competency),
        competency => other => other.DirectionId == competency.DirectionId);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListCompetencies");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateCompetency").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateCompetency").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/{id:guid}/versions", VersionsAsync).WithName("ListCompetencyVersions")
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapEntryEndpoints(Kind);
        group.MapDeleteEntry(Kind, RemoveVersionsAsync);
    }

    /// <summary>
    /// A competency goes with its history, unless a matrix uses it.
    /// </summary>
    private static async Task<string?> RemoveVersionsAsync(
        CareerCompetency competency,
        DeletionBlockers blockers,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        await blockers.AddMatricesAsync(store.MatricesUsing(competency));
        if (blockers.Any)
        {
            return InUseMessage;
        }

        var id = competency.Id;
        foreach (var version in await context.Set<CompetencyVersion>().Where(version => version.CompetencyId == id).ToListAsync(cancellationToken))
        {
            store.RemoveHistoryRow(version, $"{version.CompetencyId},{version.Number}");
        }

        return null;
    }

    /// <summary>
    /// Lists the competencies; <c>withGeneral=true</c> adds the general competencies to those of the direction, as a matrix of the direction may use them.
    /// </summary>
    private static Task<Results<Ok<PageResponse<CompetencyResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        [FromQuery(Name = CategoryField)] Guid? categoryId,
        [FromQuery(Name = DirectionField)] Guid? directionId,
        [FromQuery(Name = WithGeneralField)] bool? withGeneral,
        [FromQuery(Name = ScaleField)] Guid? scaleId,
        [FromQuery(Name = TypeField)] CompetencyType? type,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken) =>
        CatalogEndpoints.ListAsync(
            context.Set<CareerCompetency>()
                .WhereIf(categoryId is not null, competency => competency.CategoryId == categoryId)
                .WhereIf(directionId is not null, competency => competency.DirectionId == directionId || (withGeneral == true && competency.DirectionId == null))
                .WhereIf(scaleId is not null, competency => competency.ScaleId == scaleId)
                .WhereIf(type is not null, competency => competency.Type == type),
            query,
            Kind,
            store,
            cancellationToken);

    private static async Task<Results<Created<CompetencyResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CompetencyRequest request,
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

        if (await ScaleIdAsync(request, context, cancellationToken) is not { } scaleId)
        {
            return Rejection.Invalid(ScaleField, CatalogEndpoints.UnavailableMessage);
        }

        if (await ReferenceProblemAsync(request, scaleId, viewer.UserId, current: null, store, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var competency = new CareerCompetency
        {
            Name = request.TrimmedName(),
            Status = status,
            AuthorId = viewer.UserId,
        };
        request.ApplyTo(competency, scaleId);
        if (await CatalogEndpoints.NameProblemAsync(competency, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(competency);
        var created = await CatalogEndpoints.CommitAsync(competency, Kind, store, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{competency.Id}", created);
    }

    private static async Task<Results<Ok<CompetencyResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        CompetencyRequest request,
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
        if (await store.FindVisibleAsync<CareerCompetency>(id, viewer, cancellationToken) is not { } competency)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanEdit(competency))
        {
            return Rejection.Forbidden();
        }

        if (await ScaleIdAsync(request, context, cancellationToken) is not { } scaleId)
        {
            return Rejection.Invalid(ScaleField, CatalogEndpoints.UnavailableMessage);
        }

        if (await ReferenceProblemAsync(request, scaleId, competency.AuthorId, competency, store, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        ifMatch.ApplyTo(context, competency);
        request.ApplyTo(competency, scaleId);
        if (await CatalogEndpoints.NameProblemAsync(competency, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(competency, Kind, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// The competency's versions, latest first.
    /// </summary>
    private static async Task<Results<Ok<List<CompetencyVersionResponse>>, NotFound>> VersionsAsync(
        Guid id,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        var viewer = await store.ViewerAsync(cancellationToken);
        if (!await context.Set<CareerCompetency>().Where(viewer.Sees<CareerCompetency>()).AnyAsync(competency => competency.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var versions = await context.Set<CompetencyVersion>()
            .AsNoTracking()
            .Where(version => version.CompetencyId == id)
            .OrderByDescending(version => version.Number)
            .ToListAsync(cancellationToken);
        var names = await store.UserNamesAsync(versions.Select(version => version.ChangedById), cancellationToken);
        return TypedResults.Ok(versions.Select(version => new CompetencyVersionResponse(
            version.Number,
            version.Name,
            version.Description,
            version.CategoryId,
            version.DirectionId,
            version.Type,
            version.ScaleId,
            version.Status,
            version.ChangedById,
            version.ChangedById is { } userId ? names.GetValueOrDefault(userId) : null,
            version.ChangedAt)).ToList());
    }

    /// <summary>
    /// The scale the request names, or else the default scale; null when it names none and no scale is the default.
    /// </summary>
    private static async Task<Guid?> ScaleIdAsync(CompetencyRequest request, AppDbContext context, CancellationToken cancellationToken) =>
        request.ScaleId ?? await context.Set<CompetencyScale>()
            .Where(scale => scale.IsDefault)
            .Select(scale => (Guid?)scale.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Says why the category, direction or scale cannot be used: an entry may refer to active entries and to its author's drafts.
    /// Only references the change sets anew are checked, so an entry that has since been deactivated may stay where it was.
    /// </summary>
    private static async Task<ValidationProblem?> ReferenceProblemAsync(
        CompetencyRequest request,
        Guid scaleId,
        Guid? authorId,
        CareerCompetency? current,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId != current?.CategoryId
            && !await store.IsUsableAsync<CompetencyCategory>(request.CategoryId, authorId, cancellationToken))
        {
            return Rejection.Invalid(CategoryField, CatalogEndpoints.UnavailableMessage);
        }

        if (request.DirectionId is { } directionId && directionId != current?.DirectionId
            && !await store.IsUsableAsync<CareerDirection>(directionId, authorId, cancellationToken))
        {
            return Rejection.Invalid(DirectionField, CatalogEndpoints.UnavailableMessage);
        }

        if (scaleId != current?.ScaleId
            && !await store.IsUsableAsync<CompetencyScale>(scaleId, authorId, cancellationToken))
        {
            return Rejection.Invalid(ScaleField, CatalogEndpoints.UnavailableMessage);
        }

        return null;
    }
}
