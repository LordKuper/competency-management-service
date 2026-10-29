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
/// The API of competency matrices and their lists of versions. Every signed-in user reads matrices and their published and archived
/// versions; a version in work is listed only to those who may read it. Only the head of a unit creates its matrices and new versions.
/// A matrix that was never published can be deleted.
/// </summary>
internal static class MatrixEndpoints
{
    private const string Route = "/api/v1/career/matrices";
    private const string OrgUnitField = "orgUnitId";
    private const string DirectionField = "directionId";
    private const string TrackField = "trackId";
    private const string PositionField = "positionId";
    private const string DuplicateMatrixMessage = "У подразделения уже есть матрица для этого трека";
    private const string DuplicatePositionMatrixMessage = "У подразделения уже есть матрица для этой должности";
    private const string BasisMessage = "Укажите либо область и трек, либо должность";
    private const string UnavailablePositionMessage = "Должность неактивна или недоступна подразделению матрицы";
    private const string PublishedMessage = "Матрицу с опубликованными версиями удалить нельзя";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync).WithName("ListMatrices");
        group.MapPost(string.Empty, CreateAsync).WithName("CreateMatrix")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/{id:guid}", GetAsync).WithName("GetMatrix").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteMatrix")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/{id:guid}/versions", VersionsAsync).WithName("ListMatrixVersions").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/{id:guid}/versions", CreateVersionAsync).WithName("CreateMatrixVersion").ProducesETag()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// One page of matrices, oldest first, optionally of one unit, direction, track or position.
    /// </summary>
    private static async Task<Results<Ok<PageResponse<MatrixResponse>>, ValidationProblem>> ListAsync(
        [FromQuery(Name = OrgUnitField)] Guid? orgUnitId,
        [FromQuery(Name = DirectionField)] Guid? directionId,
        [FromQuery(Name = TrackField)] Guid? trackId,
        [FromQuery(Name = PositionField)] Guid? positionId,
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
            .WhereIf(directionId is not null, matrix => matrix.DirectionId == directionId)
            .WhereIf(trackId is not null, matrix => matrix.TrackId == trackId)
            .WhereIf(positionId is not null, matrix => matrix.PositionId == positionId)
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
    /// Creates a unit's matrix, for a track or for a position. A track matrix takes a track of an active direction, either an active track
    /// kept by the unit or a unit above it, or a draft track of the unit itself whoever wrote it, which publishing the version then makes
    /// active. A position matrix takes an active position the unit or a unit above it lists that no level of any track stands for; its
    /// first version gets its one column. The first version is otherwise an empty draft. A unit has one matrix per track and one per
    /// position, which the catalogue lock and unique indexes keep so; the same lock keeps a position from being linked to a level meanwhile.
    /// </summary>
    private static async Task<Results<Created<MatrixResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        NewMatrixRequest request,
        AppDbContext context,
        CatalogStore store,
        IEmployeeDirectory employees,
        MatrixWorkflow workflow,
        MatrixRoute route,
        CancellationToken cancellationToken)
    {
        if ((request.PositionId is not null) == (request.DirectionId is not null || request.TrackId is not null))
        {
            return Rejection.Invalid(PositionField, BasisMessage);
        }

        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await workflow.ViewerAsync(cancellationToken);
        if (!viewer.MatrixUnitIds.Contains(request.OrgUnitId))
        {
            return Rejection.Forbidden();
        }

        var content = MatrixContent.Empty;
        if (request.PositionId is { } positionId)
        {
            var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
            if (snapshot.Positions.GetValueOrDefault(positionId) is not { IsActive: true } || !snapshot.AvailablePositionIds(request.OrgUnitId).Contains(positionId))
            {
                return Rejection.Invalid(PositionField, UnavailablePositionMessage);
            }

            if ((await store.LinkedTracksAsync([positionId], cancellationToken))[positionId].ToList() is { Count: > 0 } linkedTracks)
            {
                var positionName = snapshot.Positions[positionId].Name;
                return Rejection.Conflict(linkedTracks.FirstOrDefault(viewer.SeesLoaded()) is { } track
                    ? $"Должность «{positionName}» связана с уровнем трека «{track.Name}» — матрица должности для неё невозможна"
                    : $"Должность «{positionName}» связана с уровнем трека — матрица должности для неё невозможна");
            }

            if (await context.Set<CompetencyMatrix>().AnyAsync(
                    matrix => matrix.OrgUnitId == request.OrgUnitId && matrix.PositionId == positionId,
                    cancellationToken))
            {
                return Rejection.Taken(PositionField, DuplicatePositionMatrixMessage);
            }

            content = content with { Levels = [new MatrixLevel(positionId, null)] };
        }
        else
        {
            if (await ReferenceProblemAsync(request, context, employees, cancellationToken) is { } invalid)
            {
                return invalid;
            }

            if (await context.Set<CompetencyMatrix>().AnyAsync(
                    matrix => matrix.OrgUnitId == request.OrgUnitId && matrix.TrackId == request.TrackId,
                    cancellationToken))
            {
                return Rejection.Taken(TrackField, DuplicateMatrixMessage);
            }
        }

        var created = new CompetencyMatrix
        {
            OrgUnitId = request.OrgUnitId,
            DirectionId = request.DirectionId,
            TrackId = request.TrackId,
            PositionId = request.PositionId,
        };
        context.Add(created);
        context.Add(new MatrixVersion
        {
            MatrixId = created.Id,
            Number = 1,
            Status = MatrixVersionStatus.Draft,
            AuthorId = viewer.UserId,
            Content = content,
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var described = (await DescribeAsync([created], viewer, context, store, route, cancellationToken))[0];
        return TypedResults.Created($"{Route}/{created.Id}", described);
    }

    /// <summary>
    /// Deletes a matrix none of whose versions was ever published, with its version in work and that version's variants and decisions,
    /// each row journaled; only the head of the matrix unit or a global administrator may. Such a matrix has one version, the one in work,
    /// and <c>If-Match</c> carries that version's version, because the matrix row itself never changes. <c>If-Match</c> is checked before
    /// anything is removed: a stale version would delete no version row, and the matrix delete in the same batch would then break the
    /// version's foreign key before EF counts the rows. The matrix lock keeps the versions read here current.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        CatalogStore store,
        MatrixWorkflow workflow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await workflow.BeginTransitionAsync(id, cancellationToken);
        if (await context.Set<CompetencyMatrix>().FirstOrDefaultAsync(matrix => matrix.Id == id, cancellationToken) is not { } matrix)
        {
            return TypedResults.NotFound();
        }

        var viewer = await workflow.ViewerAsync(cancellationToken);
        if (!viewer.IsAdmin && !viewer.MatrixUnitIds.Contains(matrix.OrgUnitId))
        {
            return Rejection.Forbidden();
        }

        var versions = await context.Set<MatrixVersion>().Where(version => version.MatrixId == id).ToListAsync(cancellationToken);
        if (versions.Any(version => !version.IsInWork))
        {
            return Rejection.Conflict(PublishedMessage);
        }

        foreach (var version in versions)
        {
            ifMatch.ApplyTo(context, version);
        }

        var versionIds = versions.Select(version => version.Id).ToList();
        foreach (var variant in await context.Set<MatrixVariant>().Where(variant => versionIds.Contains(variant.VersionId)).ToListAsync(cancellationToken))
        {
            store.RemoveHistoryRow(variant, $"{variant.VersionId},{variant.Number}");
        }

        foreach (var decision in await context.Set<MatrixDecision>().Where(decision => versionIds.Contains(decision.VersionId)).ToListAsync(cancellationToken))
        {
            store.RemoveHistoryRow(decision, $"{decision.VersionId},{decision.Sequence}");
        }

        context.RemoveRange(versions);

        context.Remove(matrix);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
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
        var versions = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(version => version.MatrixId == id)
            .OrderByDescending(version => version.Number)
            .ToListAsync(cancellationToken);
        var readable = await route.ReadableAsync(viewer, versions, cancellationToken);

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
    /// Says why the direction or track cannot be used: a new matrix needs an active direction and a track of it that is active and kept
    /// by the matrix unit or a unit above it or is a draft of the matrix unit.
    /// </summary>
    private static async Task<ValidationProblem?> ReferenceProblemAsync(
        NewMatrixRequest request,
        AppDbContext context,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        if (!await context.Set<CareerDirection>().AnyAsync(
                direction => direction.Id == request.DirectionId && direction.Status == CatalogStatus.Active,
                cancellationToken))
        {
            return Rejection.Invalid(DirectionField, CatalogEndpoints.UnavailableMessage);
        }

        var unitIds = (await employees.FindAncestryAsync(request.OrgUnitId, cancellationToken)).Select(unit => unit.Id).ToList();
        if (!await context.Set<CareerTrack>().AnyAsync(
                track => track.Id == request.TrackId
                    && track.DirectionId == request.DirectionId
                    && ((track.Status == CatalogStatus.Active && unitIds.Contains(track.OwnerOrgUnitId))
                        || (track.Status == CatalogStatus.Draft && track.OwnerOrgUnitId == request.OrgUnitId)),
                cancellationToken))
        {
            return Rejection.Invalid(TrackField, CatalogEndpoints.UnavailableMessage);
        }

        return null;
    }

    /// <summary>
    /// Describes matrices with the names of their units, directions and tracks or positions, their published versions, and their versions
    /// in work for those who may read them.
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
        var directionIds = matrices.Select(matrix => matrix.DirectionId).OfType<Guid>().Distinct().ToList();
        var trackIds = matrices.Select(matrix => matrix.TrackId).OfType<Guid>().Distinct().ToList();
        var directionNames = await context.Set<CareerDirection>().AsNoTracking()
            .Where(direction => directionIds.Contains(direction.Id))
            .ToDictionaryAsync(direction => direction.Id, direction => direction.Name, cancellationToken);
        var trackNames = await context.Set<CareerTrack>().AsNoTracking()
            .Where(track => trackIds.Contains(track.Id))
            .ToDictionaryAsync(track => track.Id, track => track.Name, cancellationToken);
        var unitNames = await store.UnitNamesAsync(matrices.Select(matrix => (Guid?)matrix.OrgUnitId), cancellationToken);
        var positionNames = await store.PositionNamesAsync(matrices.Select(matrix => matrix.PositionId), cancellationToken);
        var readableInWork = await route.ReadableAsync(viewer, [.. versions.Where(version => version.IsInWork)], cancellationToken);

        var described = new List<MatrixResponse>();
        foreach (var matrix in matrices)
        {
            var published = versions.FirstOrDefault(version => version.MatrixId == matrix.Id && version.IsPublic);
            var inWork = readableInWork.FirstOrDefault(version => version.MatrixId == matrix.Id);
            described.Add(new MatrixResponse(
                matrix.Id,
                matrix.OrgUnitId,
                unitNames.GetValueOrDefault(matrix.OrgUnitId, string.Empty),
                matrix.DirectionId,
                matrix.DirectionId is { } directionId ? directionNames.GetValueOrDefault(directionId, string.Empty) : null,
                matrix.TrackId,
                matrix.TrackId is { } trackId ? trackNames.GetValueOrDefault(trackId, string.Empty) : null,
                RefOf(published),
                RefOf(inWork),
                matrix.CreatedAt,
                matrix.PositionId,
                matrix.PositionId is { } positionId ? positionNames.GetValueOrDefault(positionId, string.Empty) : null));
        }

        return described;
    }

    private static MatrixVersionRef? RefOf(MatrixVersion? version) =>
        version is null ? null : new MatrixVersionRef(version.Id, version.Number, version.Status, version.Version);
}
