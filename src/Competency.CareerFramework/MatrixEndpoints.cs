using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of competency matrices and their lists of versions. Every signed-in user reads matrices and their published and archived
/// versions; a version in work is listed only to those who may read it. Only the head of a unit creates its matrices and new versions.
/// </summary>
internal static class MatrixEndpoints
{
    private const string Route = "/api/v1/career/matrices";
    private const string OrgUnitField = "orgUnitId";
    private const string DirectionField = "directionId";
    private const string TrackField = "trackId";
    private const string ScaleField = "scaleId";
    private const string DuplicateMatrixMessage = "У подразделения уже есть матрица для этого трека";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListMatrices");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateMatrix")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/{id:guid}", GetAsync).WithName("GetMatrix").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet("/{id:guid}/versions", VersionsAsync).WithName("ListMatrixVersions").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/{id:guid}/versions", CreateVersionAsync).WithName("CreateMatrixVersion").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// One page of matrices, oldest first, optionally of one unit.
    /// </summary>
    private static async Task<Results<Ok<PageResponse<MatrixResponse>>, ValidationProblem>> ListAsync(
        [FromQuery(Name = OrgUnitField)] Guid? orgUnitId,
        AppDbContext context,
        CatalogStore store,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = ListRequest.DefaultPageSize)
    {
        var errors = ListRequest.Validate(page, pageSize);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var matrices = await context.Set<CompetencyMatrix>()
            .AsNoTracking()
            .WhereIf(orgUnitId is not null, matrix => matrix.OrgUnitId == orgUnitId)
            .OrderBy(matrix => matrix.CreatedAt)
            .ThenBy(matrix => matrix.Id)
            .ToPageAsync(page, pageSize, cancellationToken);

        var viewer = await workflow.ViewerAsync(cancellationToken);
        var items = await DescribeAsync(matrices.Items, viewer, context, store, route, cancellationToken);
        return TypedResults.Ok(new PageResponse<MatrixResponse>(items, matrices.Total, matrices.Page, matrices.PageSize));
    }

    private static async Task<Results<Ok<MatrixResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        CatalogStore store,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        if (await context.Set<CompetencyMatrix>().AsNoTracking().FirstOrDefaultAsync(matrix => matrix.Id == id, cancellationToken) is not { } matrix)
        {
            return TypedResults.NotFound();
        }

        var viewer = await workflow.ViewerAsync(cancellationToken);
        return TypedResults.Ok((await DescribeAsync([matrix], viewer, context, store, route, cancellationToken))[0]);
    }

    /// <summary>
    /// Creates a unit's matrix for an active track of an active direction, with an empty first version as a draft. A unit has one
    /// matrix per track, which the catalogue lock and a unique index keep so.
    /// </summary>
    private static async Task<Results<Created<MatrixResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        NewMatrixRequest request,
        AppDbContext context,
        CatalogStore store,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await workflow.ViewerAsync(cancellationToken);
        if (!viewer.HeadedUnitIds.Contains(request.OrgUnitId))
        {
            return Rejection.Forbidden();
        }

        if (await ReferenceProblemAsync(request, context, store, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (await context.Set<CompetencyMatrix>().AnyAsync(
                matrix => matrix.OrgUnitId == request.OrgUnitId && matrix.TrackId == request.TrackId,
                cancellationToken))
        {
            return Rejection.Taken(TrackField, DuplicateMatrixMessage);
        }

        var created = new CompetencyMatrix { OrgUnitId = request.OrgUnitId, DirectionId = request.DirectionId, TrackId = request.TrackId };
        context.Add(created);
        context.Add(new MatrixVersion
        {
            MatrixId = created.Id,
            Number = 1,
            Status = MatrixVersionStatus.Draft,
            ScaleId = request.ScaleId,
            AuthorId = viewer.UserId,
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var described = (await DescribeAsync([created], viewer, context, store, route, cancellationToken))[0];
        return TypedResults.Created($"{Route}/{created.Id}", described);
    }

    /// <summary>
    /// The versions of a matrix the user may read, latest first.
    /// </summary>
    private static async Task<Results<Ok<List<MatrixVersionSummaryResponse>>, NotFound>> VersionsAsync(
        Guid id,
        AppDbContext context,
        CatalogStore store,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        if (!await context.Set<CompetencyMatrix>().AnyAsync(matrix => matrix.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var viewer = await workflow.ViewerAsync(cancellationToken);
        var readable = new List<MatrixVersion>();
        foreach (var version in await context.Set<MatrixVersion>()
                     .AsNoTracking()
                     .Where(version => version.MatrixId == id)
                     .OrderByDescending(version => version.Number)
                     .ToListAsync(cancellationToken))
        {
            if (await route.CanReadAsync(viewer, version, isHistory: false, cancellationToken))
            {
                readable.Add(version);
            }
        }

        var names = await store.UserNamesAsync(readable.Select(version => (Guid?)version.AuthorId), cancellationToken);
        return TypedResults.Ok(readable
            .Select(version => new MatrixVersionSummaryResponse(
                version.Id,
                version.Number,
                version.Status,
                version.AuthorId,
                names.GetValueOrDefault(version.AuthorId),
                version.CreatedAt,
                version.PublishedAt))
            .ToList());
    }

    private static Task<Results<Created<MatrixVersionResponse>, NotFound, ProblemHttpResult>> CreateVersionAsync(
        Guid id,
        MatrixWorkflow workflow,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        workflow.CreateVersionAsync(id, response, cancellationToken);

    /// <summary>
    /// Says why the direction, track or scale cannot be used: a new matrix needs an active direction and an active track of it, and a
    /// scale that is active or a draft of the unit.
    /// </summary>
    private static async Task<ValidationProblem?> ReferenceProblemAsync(
        NewMatrixRequest request,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        if (!await context.Set<CareerDirection>().AnyAsync(
                direction => direction.Id == request.DirectionId && direction.Status == CatalogStatus.Active,
                cancellationToken))
        {
            return Rejection.Invalid(DirectionField, CatalogEndpoints.UnavailableMessage);
        }

        if (!await context.Set<CareerTrack>().AnyAsync(
                track => track.Id == request.TrackId && track.DirectionId == request.DirectionId && track.Status == CatalogStatus.Active,
                cancellationToken))
        {
            return Rejection.Invalid(TrackField, CatalogEndpoints.UnavailableMessage);
        }

        return await store.IsUsableAsync<CompetencyScale>(request.ScaleId, request.OrgUnitId, cancellationToken)
            ? null
            : Rejection.Invalid(ScaleField, CatalogEndpoints.UnavailableMessage);
    }

    /// <summary>
    /// Describes matrices with the names of their units, directions and tracks, their published versions, and their versions in work
    /// for those who may read them.
    /// </summary>
    private static async Task<List<MatrixResponse>> DescribeAsync(
        IReadOnlyList<CompetencyMatrix> matrices,
        CatalogViewer viewer,
        AppDbContext context,
        CatalogStore store,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        var matrixIds = matrices.Select(matrix => matrix.Id).ToList();
        var versions = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(version => matrixIds.Contains(version.MatrixId) && version.Status != MatrixVersionStatus.Archived)
            .ToListAsync(cancellationToken);
        var directionIds = matrices.Select(matrix => matrix.DirectionId).Distinct().ToList();
        var trackIds = matrices.Select(matrix => matrix.TrackId).Distinct().ToList();
        var directionNames = await context.Set<CareerDirection>().AsNoTracking()
            .Where(direction => directionIds.Contains(direction.Id))
            .ToDictionaryAsync(direction => direction.Id, direction => direction.Name, cancellationToken);
        var trackNames = await context.Set<CareerTrack>().AsNoTracking()
            .Where(track => trackIds.Contains(track.Id))
            .ToDictionaryAsync(track => track.Id, track => track.Name, cancellationToken);
        var unitNames = await store.UnitNamesAsync(matrices.Select(matrix => (Guid?)matrix.OrgUnitId), cancellationToken);

        var described = new List<MatrixResponse>();
        foreach (var matrix in matrices)
        {
            var published = versions.FirstOrDefault(version => version.MatrixId == matrix.Id && version.IsPublic);
            var inWork = versions.FirstOrDefault(version => version.MatrixId == matrix.Id && version.IsInWork);
            if (inWork is not null && !await route.CanReadAsync(viewer, inWork, isHistory: false, cancellationToken))
            {
                inWork = null;
            }

            described.Add(new MatrixResponse(
                matrix.Id,
                matrix.OrgUnitId,
                unitNames.GetValueOrDefault(matrix.OrgUnitId, string.Empty),
                matrix.DirectionId,
                directionNames.GetValueOrDefault(matrix.DirectionId, string.Empty),
                matrix.TrackId,
                trackNames.GetValueOrDefault(matrix.TrackId, string.Empty),
                RefOf(published),
                RefOf(inWork),
                matrix.CreatedAt));
        }

        return described;
    }

    private static MatrixVersionRef? RefOf(MatrixVersion? version) =>
        version is null ? null : new MatrixVersionRef(version.Id, version.Number, version.Status);
}
