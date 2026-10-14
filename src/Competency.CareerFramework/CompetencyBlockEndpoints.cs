using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of shared blocks of competencies. Block names are unique across the catalogue. A block holds competencies of its own scale,
/// each once; an active block needs at least one and refers to active competencies only. Its scale changes only while no matrix version
/// includes it. A block no matrix version, variant or snapshot includes can be deleted.
/// </summary>
internal static class CompetencyBlockEndpoints
{
    private const string Route = "/api/v1/career/blocks";
    private const string ScaleField = "scaleId";
    private const string EmptyMessage = "Состав блока пуст";
    private const string ScaleInUseMessage = "Шкалу блока используют матрицы — изменить нельзя";
    private const string InUseMessage = "Общий блок используют матрицы — удалить нельзя";
    private const string DraftReferenceMessage = "Выбрана черновая запись — сначала утвердите её";

    internal static readonly CatalogKind<CompetencyBlock, CompetencyBlockResponse> Kind = new(
        "CompetencyBlock",
        CompetencyBlockResponse.From,
        References: block => [block.ScaleId, .. block.CompetencyIds.Select(id => (Guid?)id)]);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListCompetencyBlocks");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateCompetencyBlock").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateCompetencyBlock").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapEntryEndpoints(Kind, Route);
        group.MapDeleteEntry(Kind, InUseAsync);
    }

    private static async Task<string?> InUseAsync(
        CompetencyBlock block,
        DeletionBlockers blockers,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        await blockers.AddMatricesAsync(store.MatricesUsing(block));
        return blockers.Any ? InUseMessage : null;
    }

    private static Task<Results<Ok<PageResponse<CompetencyBlockResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        [FromQuery(Name = ScaleField)] Guid? scaleId,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken) =>
        CatalogEndpoints.ListAsync(
            context.Set<CompetencyBlock>().WhereIf(scaleId is not null, block => block.ScaleId == scaleId),
            query,
            Kind,
            store,
            cancellationToken);

    /// <summary>
    /// Creates a block: active from its keepers unless it refers to drafts, which only approving it publishes, so then it is a draft.
    /// A draft may be empty; an active block may not.
    /// </summary>
    private static async Task<Results<Created<CompetencyBlockResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CompetencyBlockRequest request,
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

        if (!await store.IsUsableAsync<CompetencyScale>(request.ScaleId, viewer.UserId, cancellationToken))
        {
            return Rejection.Invalid(ScaleField, CatalogEndpoints.UnavailableMessage);
        }

        var block = new CompetencyBlock
        {
            Name = request.TrimmedName(),
            Status = status,
            AuthorId = viewer.UserId,
            ScaleId = request.ScaleId,
            CompetencyIds = request.StoredCompetencyIds(),
        };
        if (await UnusableAsync(block.CompetencyIds, new HashSet<Guid>(), viewer.UserId, store, cancellationToken) is { } unusable)
        {
            return unusable;
        }

        if (await ForeignNameAsync(block.CompetencyIds, block.ScaleId, context, cancellationToken) is { } foreign)
        {
            return Rejection.Invalid(CompetencyBlockRequest.CompetenciesField, $"Компетенция «{foreign}» на другой шкале, чем блок");
        }

        if (block.Status == CatalogStatus.Active && await publication.HasDraftReferencesAsync(block, cancellationToken))
        {
            block.Status = CatalogStatus.Draft;
        }

        if (block.Status == CatalogStatus.Active && block.CompetencyIds.Count == 0)
        {
            return Rejection.Invalid(CompetencyBlockRequest.CompetenciesField, EmptyMessage);
        }

        if (await CatalogEndpoints.NameProblemAsync(block, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(block);
        var created = await CatalogEndpoints.CommitAsync(block, Kind, viewer, store, transaction, response, cancellationToken);
        await mail.DraftCreatedAsync(block, viewer, cancellationToken);
        return TypedResults.Created($"{Route}/{block.Id}", created);
    }

    /// <summary>
    /// Changes a block's name, scale and composition. Versions in work see the new composition at once; published ones keep theirs in their
    /// snapshot. The scale changes only while no matrix version includes the block, and only to the scale of every competency it holds.
    /// </summary>
    private static async Task<Results<Ok<CompetencyBlockResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        CompetencyBlockRequest request,
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
        if (await store.FindVisibleAsync<CompetencyBlock>(id, viewer, cancellationToken) is not { } block)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanEdit(block))
        {
            return Rejection.Forbidden();
        }

        var draftAuthorId = block.Status == CatalogStatus.Active ? null : block.AuthorId;
        var isScaleChanged = request.ScaleId != block.ScaleId;
        if (isScaleChanged && !await store.IsUsableAsync<CompetencyScale>(request.ScaleId, draftAuthorId, cancellationToken))
        {
            return Rejection.Invalid(ScaleField, CatalogEndpoints.UnavailableMessage);
        }

        var competencyIds = request.StoredCompetencyIds();
        if (await UnusableAsync(competencyIds, block.CompetencyIds.ToHashSet(), draftAuthorId, store, cancellationToken) is { } unusable)
        {
            return unusable;
        }

        if (isScaleChanged && await store.MatricesUsing(block).AnyAsync(cancellationToken))
        {
            return Rejection.Conflict(ScaleInUseMessage);
        }

        if (await ForeignNameAsync(competencyIds, request.ScaleId, context, cancellationToken) is { } foreign)
        {
            return isScaleChanged
                ? Rejection.Conflict($"Компетенция «{foreign}» не на новой шкале")
                : Rejection.Invalid(CompetencyBlockRequest.CompetenciesField, $"Компетенция «{foreign}» на другой шкале, чем блок");
        }

        if (block.Status == CatalogStatus.Active && competencyIds.Count == 0)
        {
            return Rejection.Invalid(CompetencyBlockRequest.CompetenciesField, EmptyMessage);
        }

        ifMatch.ApplyTo(context, block);
        block.Name = request.TrimmedName();
        block.ScaleId = request.ScaleId;
        block.CompetencyIds = competencyIds;
        if (await CatalogEndpoints.NameProblemAsync(block, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(block, Kind, viewer, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Rejects a competency the change adds that the block may not refer to: an active block only active ones, a draft or inactive one also
    /// its author's drafts. Competencies already held may stay, though deactivated since.
    /// </summary>
    private static async Task<ValidationProblem?> UnusableAsync(
        IReadOnlyList<Guid> competencyIds,
        IReadOnlySet<Guid> previousIds,
        Guid? draftAuthorId,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        foreach (var competencyId in competencyIds.Where(competencyId => !previousIds.Contains(competencyId)))
        {
            if (!await store.IsUsableAsync<CareerCompetency>(competencyId, draftAuthorId, cancellationToken))
            {
                var isDraft = draftAuthorId is null && await store.IsDraftAsync<CareerCompetency>(competencyId, cancellationToken);
                return Rejection.Invalid(CompetencyBlockRequest.CompetenciesField, isDraft ? DraftReferenceMessage : CatalogEndpoints.UnavailableMessage);
            }
        }

        return null;
    }

    /// <summary>
    /// The name of the first competency of the composition, by name, that is not on the scale; null when all are.
    /// </summary>
    private static Task<string?> ForeignNameAsync(List<Guid> competencyIds, Guid scaleId, AppDbContext context, CancellationToken cancellationToken) =>
        context.Set<CareerCompetency>()
            .AsNoTracking()
            .Where(competency => competencyIds.Contains(competency.Id) && competency.ScaleId != scaleId)
            .OrderBy(competency => competency.Name)
            .Select(competency => competency.Name)
            .FirstOrDefaultAsync(cancellationToken);
}
