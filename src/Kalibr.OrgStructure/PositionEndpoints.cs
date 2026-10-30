using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.OrgStructure;

/// <summary>
/// The position catalogue API. Any signed-in user reads active positions; only global administrators and HR read inactive ones
/// and create, rename, deactivate and reactivate positions, under the tree lock, because a unit's list and an employee's
/// position are checked against the catalogue under the same lock, and with <c>If-Match</c>. Positions are never deleted.
/// </summary>
internal static class PositionEndpoints
{
    private const string Route = "/api/v1/positions";
    private const string Tag = "Positions";
    private const string NameField = "name";
    private const string DuplicateNameMessage = "Должность с таким названием уже есть";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var readers = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        readers.MapGet(string.Empty, ListAsync).WithName("ListPositions");
        readers.MapGet("/{id:guid}", GetAsync).WithName("GetPosition").ProducesETag().ProducesProblem(StatusCodes.Status404NotFound);

        var keepers = readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        keepers.MapPost(string.Empty, CreateAsync).WithName("CreatePosition").ProducesETag()
            .ProducesProblem(StatusCodes.Status409Conflict);
        keepers.MapPut("/{id:guid}", RenameAsync).WithName("RenamePosition").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        keepers.MapPost("/{id:guid}/deactivate", DeactivateAsync).WithName("DeactivatePosition").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        keepers.MapPost("/{id:guid}/activate", ActivateAsync).WithName("ActivatePosition").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        keepers.MapGet("/{id:guid}/impact", ImpactAsync).WithName("GetPositionImpact")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Finds the position whose name equals the given one regardless of letter case, comparing with the database's
    /// <c>lower()</c> on both sides, as the unique index does, so the check and the index never disagree.
    /// </summary>
    /// <param name="context">The context to query through; the position found is tracked.</param>
    /// <param name="name">The trimmed name.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The position, or <see langword="null"/> when no position has that name.</returns>
    public static Task<Position?> FindByNameAsync(AppDbContext context, string name, CancellationToken cancellationToken) =>
        context.Set<Position>()
            .FromSql($"SELECT * FROM positions WHERE lower(name) = lower({name})")
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Saves the position, commits, and returns it with its new version as the <c>ETag</c>.
    /// </summary>
    public static async Task<PositionResponse> SaveAsync(
        Position position,
        AppDbContext context,
        IDbContextTransaction transaction,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        response.SetETag(position.Version);
        return PositionResponse.From(position);
    }

    private static async Task<Results<Ok<PageResponse<PositionResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] PositionListQuery query,
        AppDbContext context,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var text = TextSearch.Normalize(query.Q);
        var pattern = text is null ? null : ListRequest.ContainsPattern(text);
        var page = await context.Set<Position>()
            .AsNoTracking()
            .VisibleTo(actor)
            .WhereIf(query.IsActive is not null, position => position.IsActive == query.IsActive)
            .WhereIf(
                text is not null,
                position => EF.Functions.ILike(position.Name, pattern!, ListRequest.LikeEscape)
                    || position.SearchVector.Matches(EF.Functions.PlainToTsQuery(TextSearch.FullTextConfig, text!)))
            .OrderBy(position => position.Name)
            .ThenBy(position => position.Id)
            .Select(PositionResponse.Projection)
            .ToPageAsync(query.Page, query.PageSize, cancellationToken);

        return TypedResults.Ok(page);
    }

    private static async Task<Results<Ok<PositionResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        ICurrentActor actor,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var position = await context.Set<Position>()
            .AsNoTracking()
            .VisibleTo(actor)
            .Where(candidate => candidate.Id == id)
            .Select(PositionResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);
        if (position is null)
        {
            return TypedResults.NotFound();
        }

        response.SetETag(position.Version);
        return TypedResults.Ok(position);
    }

    private static async Task<Results<Created<PositionResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        PositionRequest request,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var name = request.TrimmedName();
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        if (await FindByNameAsync(context, name, cancellationToken) is not null)
        {
            return Rejection.Taken(NameField, DuplicateNameMessage);
        }

        var position = new Position { Name = name };
        context.Add(position);

        var created = await SaveAsync(position, context, transaction, response, cancellationToken);
        return TypedResults.Created($"{Route}/{created.Id}", created);
    }

    private static async Task<Results<Ok<PositionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> RenameAsync(
        Guid id,
        PositionRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var name = request.TrimmedName();
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var position = await context.Set<Position>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (position is null)
        {
            return TypedResults.NotFound();
        }

        if (await FindByNameAsync(context, name, cancellationToken) is { } namesake && namesake.Id != id)
        {
            return Rejection.Taken(NameField, DuplicateNameMessage);
        }

        ifMatch.ApplyTo(context, position);
        position.Name = name;

        return TypedResults.Ok(await SaveAsync(position, context, transaction, response, cancellationToken));
    }

    private static Task<Results<Ok<PositionResponse>, NotFound>> DeactivateAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        SetActiveAsync(id, isActive: false, ifMatch, context, response, cancellationToken);

    private static Task<Results<Ok<PositionResponse>, NotFound>> ActivateAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        SetActiveAsync(id, isActive: true, ifMatch, context, response, cancellationToken);

    /// <summary>
    /// Changes the status alone: the employees who hold the position keep it either way, and the units keep it in their lists.
    /// </summary>
    private static async Task<Results<Ok<PositionResponse>, NotFound>> SetActiveAsync(
        Guid id,
        bool isActive,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var position = await context.Set<Position>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (position is null)
        {
            return TypedResults.NotFound();
        }

        ifMatch.ApplyTo(context, position);
        position.IsActive = isActive;

        return TypedResults.Ok(await SaveAsync(position, context, transaction, response, cancellationToken));
    }

    /// <summary>
    /// The number of working employees who hold the position in any of their assignments, shown before it is deactivated; read-only.
    /// </summary>
    private static async Task<Results<Ok<PositionImpactResponse>, NotFound>> ImpactAsync(
        Guid id,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Set<Position>().AnyAsync(candidate => candidate.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var count = await context.WorkingPlacements()
            .Where(placement => placement.PositionId == id)
            .CountEmployeesAsync(cancellationToken);
        return TypedResults.Ok(new PositionImpactResponse(count));
    }
}
