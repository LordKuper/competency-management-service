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
/// scale gets the default scale. A competency a shared block holds keeps its scale, as the block's competencies share the block's. A
/// competency no matrix and no shared block uses can be deleted with its history.
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
    private const string BlockInUseMessage = "Компетенцию используют общие блоки — удалить нельзя";
    private const string BlockScaleMessage = "Компетенцию используют общие блоки — сменить шкалу нельзя";
    private const string DraftReferenceMessage = "Выбрана черновая запись — сначала утвердите её";

    internal static readonly CatalogKind<CareerCompetency, CompetencyResponse> Kind = new(
        "Competency",
        CompetencyResponse.From,
        competency => other => other.DirectionId == competency.DirectionId,
        competency => [competency.CategoryId, competency.DirectionId, competency.ScaleId]);

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
        group.MapEntryEndpoints(Kind, Route);
        group.MapDeleteEntry(Kind, RemoveVersionsAsync);
    }

    /// <summary>
    /// A competency goes with its history, unless a matrix uses it, directly or through a shared block, or a shared block holds it.
    /// </summary>
    private static async Task<string?> RemoveVersionsAsync(
        CareerCompetency competency,
        DeletionBlockers blockers,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        await blockers.AddMatricesAsync(store.MatricesUsing(competency));
        var isUsedByMatrices = blockers.Any;
        await blockers.AddAsync(store.BlocksHolding(competency), DeletionBlockerKind.Block);
        if (blockers.Any)
        {
            return isUsedByMatrices ? InUseMessage : BlockInUseMessage;
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

    /// <summary>
    /// Creates a competency: active from its keepers unless it refers to drafts, which only approving it publishes, so then it is a draft.
    /// </summary>
    private static async Task<Results<Created<CompetencyResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CompetencyRequest request,
        AppDbContext context,
        CatalogStore store,
        CatalogPublication publication,
        CareerMail mail,
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
        if (competency.Status == CatalogStatus.Active && await publication.HasDraftReferencesAsync(competency, cancellationToken))
        {
            competency.Status = CatalogStatus.Draft;
        }

        if (await CatalogEndpoints.NameProblemAsync(competency, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(competency);
        var created = await CatalogEndpoints.CommitAsync(competency, Kind, viewer, store, transaction, response, cancellationToken);
        await mail.DraftCreatedAsync(competency, viewer, cancellationToken);
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

        var draftAuthorId = competency.Status == CatalogStatus.Active ? null : competency.AuthorId;
        if (await ReferenceProblemAsync(request, scaleId, draftAuthorId, competency, store, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (scaleId != competency.ScaleId && await store.BlocksHolding(competency).AnyAsync(cancellationToken))
        {
            return Rejection.Conflict(BlockScaleMessage);
        }

        ifMatch.ApplyTo(context, competency);
        request.ApplyTo(competency, scaleId);
        if (await CatalogEndpoints.NameProblemAsync(competency, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(competency, Kind, viewer, store, transaction, response, cancellationToken));
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
    /// Says why the category, direction or scale cannot be used: an entry may refer to active entries, and a draft or inactive one also to
    /// its author's drafts, so an active competency never gets a draft reference past publication. Only references the change sets anew are
    /// checked, so an entry that has since been deactivated may stay where it was.
    /// </summary>
    private static async Task<ValidationProblem?> ReferenceProblemAsync(
        CompetencyRequest request,
        Guid scaleId,
        Guid? draftAuthorId,
        CareerCompetency? current,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId != current?.CategoryId
            && await UnusableAsync<CompetencyCategory>(CategoryField, request.CategoryId, draftAuthorId, store, cancellationToken) is { } category)
        {
            return category;
        }

        if (request.DirectionId is { } directionId && directionId != current?.DirectionId
            && await UnusableAsync<CareerDirection>(DirectionField, directionId, draftAuthorId, store, cancellationToken) is { } direction)
        {
            return direction;
        }

        return scaleId != current?.ScaleId
            ? await UnusableAsync<CompetencyScale>(ScaleField, scaleId, draftAuthorId, store, cancellationToken)
            : null;
    }

    /// <summary>
    /// Rejects a reference the competency may not have; a draft where only an active entry may stand gets a reason of its own.
    /// </summary>
    private static async Task<ValidationProblem?> UnusableAsync<T>(
        string field,
        Guid id,
        Guid? draftAuthorId,
        CatalogStore store,
        CancellationToken cancellationToken)
        where T : CatalogEntry
    {
        if (await store.IsUsableAsync<T>(id, draftAuthorId, cancellationToken))
        {
            return null;
        }

        var isDraft = draftAuthorId is null && await store.IsDraftAsync<T>(id, cancellationToken);
        return Rejection.Invalid(field, isDraft ? DraftReferenceMessage : CatalogEndpoints.UnavailableMessage);
    }
}
