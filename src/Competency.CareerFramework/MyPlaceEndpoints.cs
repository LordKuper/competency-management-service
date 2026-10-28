using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API that tells the signed-in user where they stand in the company and on their career tracks, or what the position matrix of their
/// position requires. It answers only about the user and shows only what every employee of their unit reads: the published matrix
/// versions of that unit, never of other units or unpublished. The user's level comes from their position alone.
/// </summary>
internal static class MyPlaceEndpoints
{
    private const string Route = "/api/v1/career/me/place";
    private const string Tag = "Career";

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapGet(Route, GetAsync)
        .WithTags(Tag)
        .WithName("GetMyPlace")
        .RequireAuthorization(AuthorizationPolicies.Authenticated)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<Results<Ok<MyPlaceResponse>, UnauthorizedHttpResult>> GetAsync(
        ICurrentActor actor,
        AppDbContext context,
        IEmployeeDirectory employees,
        IUserDirectory users,
        ProcessRoles roles,
        CancellationToken cancellationToken)
    {
        if (actor.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (await users.FindAsync(userId, cancellationToken) is not { EmployeeId: { } employeeId }
            || await employees.FindAsync(employeeId, cancellationToken) is not { } employee)
        {
            return TypedResults.Ok(new MyPlaceResponse(null, [], null, null, null, [], [], NoTrack: false, PositionMatrix: null));
        }

        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        var ancestry = snapshot.Ancestry(employee.OrgUnitId);
        var head = ancestry is [{ HeadEmployeeId: { } headId }, ..] && await employees.FindAsync(headId, cancellationToken) is { IsActive: true } found
            ? new CareerEmployeeResponse(found.Id, found.FullName)
            : null;
        var assigneeOf = await roles.AssigneesAsync([.. ancestry.Select(unit => unit.Id)], cancellationToken);
        var hr = await OrgUnitHrEndpoints.DescribeAsync(ProcessRoles.NearestAssigned(ancestry, assigneeOf), assigneeOf, employees, users, cancellationToken);

        var published = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(version => version.Status == MatrixVersionStatus.Published && version.Matrix.OrgUnitId == employee.OrgUnitId)
            .ToListAsync(cancellationToken);
        var tracks = await TracksAsync(employee.PositionId, published, context, cancellationToken);
        var positionMatrix = published.FirstOrDefault(version => version.Matrix.PositionId == employee.PositionId) is { } positionVersion
            ? PositionMatrixOf(positionVersion, employee.PositionId)
            : null;

        return TypedResults.Ok(new MyPlaceResponse(
            new CareerEmployeeResponse(employee.Id, employee.FullName),
            [.. ancestry.Reverse().Select(unit => new CareerUnitResponse(unit.Id, unit.Name))],
            snapshot.Positions.TryGetValue(employee.PositionId, out var position) ? new CareerNamedResponse(position.Id, position.Name) : null,
            head,
            OrgUnitHrEndpoints.Acting(hr),
            tracks,
            [.. published
                .Select(version => version.Matrix.TrackId is { } trackId ? MatrixOf(version, trackId) : null)
                .OfType<MyPlaceMatrixResponse>()
                .OrderBy(matrix => matrix.TrackName, TextCollation.Comparer)
                .ThenBy(matrix => matrix.MatrixId)],
            NoTrack: tracks.Count == 0 && positionMatrix is null,
            positionMatrix));
    }

    /// <summary>
    /// The active tracks with an active level the position is linked to, in name order. A position is linked to one level of a track at
    /// most, so the level is the user's on that track. Requirements come from the published version of the user's unit for the track.
    /// </summary>
    private static async Task<List<MyPlaceTrackResponse>> TracksAsync(
        Guid positionId,
        IReadOnlyList<MatrixVersion> published,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        var links = context.Set<CareerLevelPosition>();
        var currentLevels = await context.Set<CareerLevel>()
            .AsNoTracking()
            .Where(level => level.Status == CatalogStatus.Active
                && level.Track.Status == CatalogStatus.Active
                && links.Any(link => link.LevelId == level.Id && link.PositionId == positionId))
            .ToListAsync(cancellationToken);
        var trackIds = currentLevels.Select(level => level.TrackId).ToList();
        var trackLevels = (await context.Set<CareerLevel>()
                .AsNoTracking()
                .Where(level => trackIds.Contains(level.TrackId) && level.Status == CatalogStatus.Active)
                .OrderBy(level => level.Position)
                .ToListAsync(cancellationToken))
            .ToLookup(level => level.TrackId);
        var directionIds = currentLevels.Select(level => level.Track.DirectionId).Distinct().ToList();
        var directionNames = await context.Set<CareerDirection>()
            .Where(direction => directionIds.Contains(direction.Id))
            .ToDictionaryAsync(direction => direction.Id, direction => direction.Name, cancellationToken);

        return [.. currentLevels
            .OrderBy(level => level.Track.Name, TextCollation.Comparer)
            .ThenBy(level => level.TrackId)
            .Select(current =>
            {
                var track = current.Track;
                var version = published.FirstOrDefault(candidate => candidate.Matrix.TrackId == track.Id);
                return new MyPlaceTrackResponse(
                    new CareerNamedResponse(track.Id, track.Name),
                    new CareerNamedResponse(track.DirectionId, directionNames.GetValueOrDefault(track.DirectionId, string.Empty)),
                    new CareerNamedResponse(current.Id, current.Name),
                    version is null ? null : MatrixOf(version, track.Id),
                    [.. trackLevels[track.Id]
                        .Where(level => level.Position > current.Position)
                        .Select(level => new MyPlaceLevelResponse(level.Id, level.Name, version is null ? null : RequirementsOf(version, level.Id)))]);
            })];
    }

    /// <summary>
    /// What the published version requires in one column, of the competencies it counts, each score labelled by the scale the snapshot
    /// froze for the competency.
    /// </summary>
    private static List<MyPlaceRequirementResponse> RequirementsOf(MatrixVersion version, Guid levelId)
    {
        var snapshot = version.Snapshot;
        var competencies = snapshot?.Competencies.ToDictionary(competency => competency.Id) ?? [];
        var scalePoints = snapshot?.Scales.ToDictionary(scale => scale.Id, scale => scale.Points) ?? [];
        var counted = version.Content.CompetenciesWith(version.CompositionsOf(new Dictionary<Guid, IReadOnlyList<Guid>>()))
            .Select(item => item.CompetencyId)
            .ToHashSet();
        return [.. version.Content.Requirements
            .Where(requirement => requirement.LevelId == levelId && counted.Contains(requirement.CompetencyId))
            .Select(requirement => (Requirement: requirement, Competency: competencies.GetValueOrDefault(requirement.CompetencyId)))
            .Select(pair => new MyPlaceRequirementResponse(
                pair.Requirement.CompetencyId,
                pair.Competency?.Name ?? string.Empty,
                pair.Requirement.RequiredScore,
                pair.Competency?.ScaleId is { } scaleId && scalePoints.TryGetValue(scaleId, out var points)
                    ? points.FirstOrDefault(point => point.Score == pair.Requirement.RequiredScore)?.Label
                    : null,
                pair.Competency?.Type ?? CompetencyType.Additional))];
    }

    private static MyPlaceMatrixResponse MatrixOf(MatrixVersion version, Guid trackId) => new(
        version.MatrixId,
        trackId,
        version.Snapshot?.Track?.Name ?? string.Empty,
        version.Id,
        version.Number,
        version.PublishedAt);

    private static MyPlacePositionMatrixResponse PositionMatrixOf(MatrixVersion version, Guid positionId) => new(
        version.MatrixId,
        version.Snapshot?.Position?.Name ?? string.Empty,
        version.Id,
        version.Number,
        version.PublishedAt,
        RequirementsOf(version, positionId));
}
