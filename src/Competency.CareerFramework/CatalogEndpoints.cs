using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.CareerFramework;

/// <summary>
/// The endpoint steps every career catalogue shares. Reading an entry the user cannot see answers 404, changing one they see without
/// the right answers 403. Every change takes the catalogue write lock, works out the user's rights under it, and needs <c>If-Match</c>
/// unless it creates an entry.
/// </summary>
internal static class CatalogEndpoints
{
    public const string NameField = "name";
    public const string UnavailableMessage = "Запись не найдена или недоступна";

    private const string Tag = "Career";
    public const string DuplicateNameMessage = "Запись с таким названием уже есть";
    private const string NotDraftMessage = "Согласовать можно только черновую запись";
    private const string DraftActivationMessage = "Черновую запись нужно согласовать";
    private const string ApprovedAction = "Approved";

    public static RouteGroupBuilder MapCatalogGroup(this IEndpointRouteBuilder endpoints, string route) => endpoints.MapGroup(route)
        .WithTags(Tag)
        .RequireAuthorization(AuthorizationPolicies.Authenticated)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

    /// <summary>
    /// Maps the whole API of a catalogue whose entries are a name alone: the list, creation and the entry endpoints.
    /// </summary>
    public static void MapNamedCatalog<TEntry>(
        this IEndpointRouteBuilder endpoints,
        string route,
        string listName,
        CatalogKind<TEntry, CatalogEntryResponse> kind,
        Func<CatalogNameRequest, CatalogStatus, Guid, TEntry> create)
        where TEntry : CatalogEntry
    {
        var group = endpoints.MapCatalogGroup(route);
        group.MapGet(
                string.Empty,
                ([AsParameters] CatalogListQuery query, AppDbContext context, CatalogStore store, CancellationToken cancellationToken) =>
                    ListAsync(context.Set<TEntry>(), query, kind, store, cancellationToken))
            .WithName(listName);
        group.MapPost(
                string.Empty,
                (CatalogNameRequest request, AppDbContext context, CatalogStore store, HttpResponse response, CancellationToken cancellationToken) =>
                    CreateNamedAsync(request, route, create, kind, context, store, response, cancellationToken))
            .WithName($"Create{kind.Name}")
            .ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapEntryEndpoints(kind, mapRename: true);
    }

    /// <summary>
    /// Maps reading one entry, renaming it when <paramref name="mapRename"/> is set, and its status changes: approving a draft,
    /// deactivating and reactivating.
    /// </summary>
    public static void MapEntryEndpoints<TEntry, TResponse>(this RouteGroupBuilder group, CatalogKind<TEntry, TResponse> kind, bool mapRename = false)
        where TEntry : CatalogEntry
    {
        group.MapGet(
                "/{id:guid}",
                (Guid id, CatalogStore store, HttpResponse response, CancellationToken cancellationToken) =>
                    GetAsync(id, kind, store, response, cancellationToken))
            .WithName($"Get{kind.Name}")
            .ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);

        if (mapRename)
        {
            group.MapPut(
                    "/{id:guid}",
                    (Guid id, CatalogNameRequest request, IfMatch ifMatch, AppDbContext context, CatalogStore store, HttpResponse response, CancellationToken cancellationToken) =>
                        RenameAsync(id, request, ifMatch, kind, context, store, response, cancellationToken))
                .WithName($"Update{kind.Name}")
                .ProducesETag()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        group.MapPost(
                "/{id:guid}/approve",
                (Guid id, IfMatch ifMatch, AppDbContext context, CatalogStore store, IAuditWriter audit, HttpResponse response, CancellationToken cancellationToken) =>
                    ApproveAsync(id, ifMatch, kind, context, store, audit, response, cancellationToken))
            .WithName($"Approve{kind.Name}")
            .ProducesStatusChange();

        group.MapPost(
                "/{id:guid}/deactivate",
                (Guid id, IfMatch ifMatch, AppDbContext context, CatalogStore store, HttpResponse response, CancellationToken cancellationToken) =>
                    SetStatusAsync(id, CatalogStatus.Inactive, ifMatch, kind, context, store, response, cancellationToken))
            .WithName($"Deactivate{kind.Name}")
            .ProducesStatusChange();

        group.MapPost(
                "/{id:guid}/activate",
                (Guid id, IfMatch ifMatch, AppDbContext context, CatalogStore store, HttpResponse response, CancellationToken cancellationToken) =>
                    SetStatusAsync(id, CatalogStatus.Active, ifMatch, kind, context, store, response, cancellationToken))
            .WithName($"Activate{kind.Name}")
            .ProducesStatusChange();
    }

    /// <summary>
    /// Reads one page of the entries of <paramref name="source"/> the user can read, in name order.
    /// </summary>
    public static async Task<Results<Ok<PageResponse<TResponse>>, ValidationProblem>> ListAsync<TEntry, TResponse>(
        IQueryable<TEntry> source,
        CatalogListQuery query,
        CatalogKind<TEntry, TResponse> kind,
        CatalogStore store,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var viewer = await store.ViewerAsync(cancellationToken);
        var pattern = query.NamePattern();
        var page = await source
            .AsNoTracking()
            .Where(viewer.Sees<TEntry>())
            .WhereIf(query.Status is not null, entry => entry.Status == query.Status)
            .WhereIf(pattern is not null, entry => EF.Functions.ILike(entry.Name, pattern!, ListRequest.LikeEscape))
            .OrderBy(entry => entry.Name)
            .ThenBy(entry => entry.Id)
            .ToPageAsync(query.Page, query.PageSize, cancellationToken);

        var unitNames = await store.UnitNamesAsync(page.Items.Select(entry => entry.UnitId), cancellationToken);
        var items = page.Items.Select(entry => kind.Describe(entry, NameOf(entry.UnitId, unitNames))).ToList();
        return TypedResults.Ok(new PageResponse<TResponse>(items, page.Total, page.Page, page.PageSize));
    }

    /// <summary>
    /// Rejects the entry about to be saved when another entry it competes with has its name.
    /// </summary>
    public static async Task<ProblemHttpResult?> NameProblemAsync<TEntry, TResponse>(
        TEntry entry,
        CatalogKind<TEntry, TResponse> kind,
        CatalogStore store,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry =>
        await store.IsNameTakenAsync(entry, kind.SameScope?.Invoke(entry), cancellationToken)
            ? Rejection.Taken(NameField, DuplicateNameMessage)
            : null;

    /// <summary>
    /// Saves the created or changed entry, commits, and returns it with its new version as the <c>ETag</c>.
    /// </summary>
    public static async Task<TResponse> CommitAsync<TEntry, TResponse>(
        TEntry entry,
        CatalogKind<TEntry, TResponse> kind,
        CatalogStore store,
        IDbContextTransaction transaction,
        HttpResponse response,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
    {
        await store.SaveAsync(entry, transaction, cancellationToken);
        return await DescribeAsync(entry, kind, store, response, cancellationToken);
    }

    private static async Task<TResponse> DescribeAsync<TEntry, TResponse>(
        TEntry entry,
        CatalogKind<TEntry, TResponse> kind,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
    {
        var unitNames = await store.UnitNamesAsync([entry.UnitId], cancellationToken);
        response.SetETag(entry.Version);
        return kind.Describe(entry, NameOf(entry.UnitId, unitNames));
    }

    private static string? NameOf(Guid? unitId, IReadOnlyDictionary<Guid, string> unitNames) =>
        unitId is { } id ? unitNames.GetValueOrDefault(id) : null;

    private static RouteHandlerBuilder ProducesStatusChange(this RouteHandlerBuilder builder) => builder
        .ProducesETag()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<TResponse>, NotFound>> GetAsync<TEntry, TResponse>(
        Guid id,
        CatalogKind<TEntry, TResponse> kind,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
    {
        var viewer = await store.ViewerAsync(cancellationToken);
        var entry = await store.FindVisibleAsync<TEntry>(id, viewer, cancellationToken);
        return entry is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await DescribeAsync(entry, kind, store, response, cancellationToken));
    }

    private static async Task<Results<Created<CatalogEntryResponse>, ValidationProblem, ProblemHttpResult>> CreateNamedAsync<TEntry>(
        CatalogNameRequest request,
        string route,
        Func<CatalogNameRequest, CatalogStatus, Guid, TEntry> create,
        CatalogKind<TEntry, CatalogEntryResponse> kind,
        AppDbContext context,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
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

        var entry = create(request, status, viewer.UserId);
        if (await NameProblemAsync(entry, kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        context.Add(entry);
        var created = await CommitAsync(entry, kind, store, transaction, response, cancellationToken);
        return TypedResults.Created($"{route}/{entry.Id}", created);
    }

    private static async Task<Results<Ok<TResponse>, NotFound, ValidationProblem, ProblemHttpResult>> RenameAsync<TEntry, TResponse>(
        Guid id,
        CatalogNameRequest request,
        IfMatch ifMatch,
        CatalogKind<TEntry, TResponse> kind,
        AppDbContext context,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<TEntry>(id, viewer, cancellationToken) is not { } entry)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanEdit(entry))
        {
            return Rejection.Forbidden();
        }

        ifMatch.ApplyTo(context, entry);
        entry.Name = request.TrimmedName();
        if (await NameProblemAsync(entry, kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CommitAsync(entry, kind, store, transaction, response, cancellationToken));
    }

    /// <summary>
    /// Makes a draft active and journals the approval as an event of its own; approving a track approves its draft levels with it.
    /// Names are checked again, because the drafts of other authors did not compete with them while they were drafts.
    /// </summary>
    private static async Task<Results<Ok<TResponse>, NotFound, ProblemHttpResult>> ApproveAsync<TEntry, TResponse>(
        Guid id,
        IfMatch ifMatch,
        CatalogKind<TEntry, TResponse> kind,
        AppDbContext context,
        CatalogStore store,
        IAuditWriter audit,
        HttpResponse response,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
    {
        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<TEntry>(id, viewer, cancellationToken) is not { } entry)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanManage(entry))
        {
            return Rejection.Forbidden();
        }

        if (entry.Status != CatalogStatus.Draft)
        {
            return Rejection.Conflict(NotDraftMessage);
        }

        ifMatch.ApplyTo(context, entry);
        entry.Status = CatalogStatus.Active;
        if (await NameProblemAsync(entry, kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        if (entry is CareerTrack track && await ApproveLevelsAsync(track, context, store, audit, cancellationToken) is { } levelTaken)
        {
            return levelTaken;
        }

        StageApproval(entry, context, audit);
        return TypedResults.Ok(await CommitAsync(entry, kind, store, transaction, response, cancellationToken));
    }

    private static async Task<ProblemHttpResult?> ApproveLevelsAsync(
        CareerTrack track,
        AppDbContext context,
        CatalogStore store,
        IAuditWriter audit,
        CancellationToken cancellationToken)
    {
        var drafts = await context.Set<CareerLevel>()
            .Where(level => level.TrackId == track.Id && level.Status == CatalogStatus.Draft)
            .ToListAsync(cancellationToken);
        foreach (var level in drafts)
        {
            level.Status = CatalogStatus.Active;
            if (await store.IsNameTakenAsync(level, CareerLevelEndpoints.Kind.SameScope?.Invoke(level), cancellationToken))
            {
                return Rejection.Conflict($"{DuplicateNameMessage}: «{level.Name}»");
            }

            StageApproval(level, context, audit);
        }

        return null;
    }

    private static void StageApproval(CatalogEntry entry, AppDbContext context, IAuditWriter audit)
    {
        var entityType = entry.GetType().Name;
        audit.Stage(new AuditEntry($"{entityType}.{ApprovedAction}", entityType, entry.Id.ToString()), context);
    }

    /// <summary>
    /// Deactivates an entry or brings an inactive one back; setting the status an entry already has changes nothing.
    /// A draft becomes active only by approval. A deactivated draft stops being a draft, so its name is checked like an approved one's.
    /// </summary>
    private static async Task<Results<Ok<TResponse>, NotFound, ProblemHttpResult>> SetStatusAsync<TEntry, TResponse>(
        Guid id,
        CatalogStatus status,
        IfMatch ifMatch,
        CatalogKind<TEntry, TResponse> kind,
        AppDbContext context,
        CatalogStore store,
        HttpResponse response,
        CancellationToken cancellationToken)
        where TEntry : CatalogEntry
    {
        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<TEntry>(id, viewer, cancellationToken) is not { } entry)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanManage(entry))
        {
            return Rejection.Forbidden();
        }

        if (entry.Status == CatalogStatus.Draft && status == CatalogStatus.Active)
        {
            return Rejection.Conflict(DraftActivationMessage);
        }

        var leavesDraft = entry.Status == CatalogStatus.Draft;
        ifMatch.ApplyTo(context, entry);
        entry.Status = status;
        if (leavesDraft && await NameProblemAsync(entry, kind, store, cancellationToken) is { } taken)
        {
            return taken;
        }

        return TypedResults.Ok(await CommitAsync(entry, kind, store, transaction, response, cancellationToken));
    }
}
