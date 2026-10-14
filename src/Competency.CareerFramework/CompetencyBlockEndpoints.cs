using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of shared blocks of competencies. Block names are unique across the catalogue. A block has no scale: it holds competencies of
/// any scales, each once; an active block needs at least one and refers to active competencies only. A block applies to all matrices or
/// to those of one active unit and the units below it; a unit head proposes and edits only drafts that apply to a unit of the subtrees
/// they head. A block no matrix version, variant or snapshot includes can be deleted.
/// </summary>
internal static class CompetencyBlockEndpoints
{
    private const string Route = "/api/v1/career/blocks";
    private const string ScopeField = "scopeUnitId";
    private const string EmptyMessage = "Состав блока пуст";
    private const string InUseMessage = "Общий блок используют матрицы — удалить нельзя";
    private const string DraftReferenceMessage = "Выбрана черновая запись — сначала утвердите её";

    internal static readonly CatalogKind<CompetencyBlock, CompetencyBlockResponse> Kind = new(
        "CompetencyBlock",
        CompetencyBlockResponse.From,
        References: block => [.. block.CompetencyIds.Select(id => (Guid?)id)]);

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

    /// <summary>
    /// Lists the blocks the user can read: optionally applying to one unit, applying to all (<c>forAll=true</c>) or to a
    /// unit (<c>forAll=false</c>), and available to the matrices of a unit (<c>availableFor</c>): those applying to all, to the unit or to a
    /// unit above it.
    /// </summary>
    private static async Task<Results<Ok<PageResponse<CompetencyBlockResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] CatalogListQuery query,
        [FromQuery(Name = ScopeField)] Guid? scopeUnitId,
        [FromQuery(Name = "forAll")] bool? forAll,
        [FromQuery(Name = "availableFor")] Guid? availableFor,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        List<Guid> availableScopeIds = availableFor is { } unitId
            ? [.. (await employees.FindAncestryAsync(unitId, cancellationToken)).Select(unit => unit.Id)]
            : [];
        return await CatalogEndpoints.ListAsync(
            context.Set<CompetencyBlock>()
                .WhereIf(scopeUnitId is not null, block => block.ScopeUnitId == scopeUnitId)
                .WhereIf(forAll is not null, block => (block.ScopeUnitId == null) == forAll)
                .WhereIf(availableFor is not null, block => block.ScopeUnitId == null || availableScopeIds.Contains(block.ScopeUnitId.Value)),
            query,
            Kind,
            store,
            cancellationToken);
    }

    /// <summary>
    /// Creates a block: active from its keepers unless it refers to drafts, which only approving it publishes, so then it is a draft, and
    /// a draft from a unit head, for a unit of the subtrees they head. A draft may be empty; an active block may not.
    /// </summary>
    private static async Task<Results<Created<CompetencyBlockResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CompetencyBlockRequest request,
        AppDbContext context,
        CatalogStore store,
        CatalogPublication publication,
        CareerMail mail,
        IEmployeeDirectory employees,
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
        if (viewer.StatusOfNewBlock(request.ScopeUnitId) is not { } status)
        {
            return Rejection.Forbidden();
        }

        if (await ScopeProblemAsync(request.ScopeUnitId, employees, cancellationToken) is { } invalidScope)
        {
            return invalidScope;
        }

        var block = new CompetencyBlock
        {
            Name = request.TrimmedName(),
            Status = status,
            AuthorId = viewer.UserId,
            ScopeUnitId = request.ScopeUnitId,
            CompetencyIds = request.StoredCompetencyIds(),
        };
        if (await UnusableAsync(block.CompetencyIds, new HashSet<Guid>(), viewer.UserId, store, cancellationToken) is { } unusable)
        {
            return unusable;
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
    /// Changes a block's name, unit and composition. Versions in work see the new composition at once; published ones keep theirs in their
    /// snapshot. The unit changes whoever uses the block, because a version in work that can no longer include it is stopped by
    /// completeness; a unit head moves their draft only within the subtrees they head.
    /// </summary>
    private static async Task<Results<Ok<CompetencyBlockResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        CompetencyBlockRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
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

        if (!viewer.CanEdit(block) || (!viewer.CanManage(block) && !viewer.HeadsSubtreeOf(request.ScopeUnitId)))
        {
            return Rejection.Forbidden();
        }

        if (request.ScopeUnitId != block.ScopeUnitId && await ScopeProblemAsync(request.ScopeUnitId, employees, cancellationToken) is { } invalidScope)
        {
            return invalidScope;
        }

        var draftAuthorId = block.Status == CatalogStatus.Active ? null : block.AuthorId;
        var competencyIds = request.StoredCompetencyIds();
        if (await UnusableAsync(competencyIds, block.CompetencyIds.ToHashSet(), draftAuthorId, store, cancellationToken) is { } unusable)
        {
            return unusable;
        }

        if (block.Status == CatalogStatus.Active && competencyIds.Count == 0)
        {
            return Rejection.Invalid(CompetencyBlockRequest.CompetenciesField, EmptyMessage);
        }

        ifMatch.ApplyTo(context, block);
        block.Name = request.TrimmedName();
        block.ScopeUnitId = request.ScopeUnitId;
        block.CompetencyIds = competencyIds;
        if (await CatalogEndpoints.NameProblemAsync(block, Kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CatalogEndpoints.CommitAsync(block, Kind, viewer, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Rejects a unit a block cannot apply to: one that does not exist or is no longer active.
    /// </summary>
    private static async Task<ValidationProblem?> ScopeProblemAsync(Guid? scopeUnitId, IEmployeeDirectory employees, CancellationToken cancellationToken) =>
        scopeUnitId is { } unitId && await employees.FindUnitAsync(unitId, cancellationToken) is not { IsActive: true }
            ? Rejection.Invalid(ScopeField, CatalogEndpoints.UnavailableMessage)
            : null;

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
}
