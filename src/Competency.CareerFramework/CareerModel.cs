using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// Answers <see cref="ICareerModel"/> from the published track matrices, which a unit inherits where it has none of its own
/// (<see cref="MatrixInheritance"/>), and the process roles, on the request's database context. A version is read from its publication
/// snapshot only, so catalogue edits after publication never reach it.
/// </summary>
internal sealed class CareerModel(AppDbContext context, ProcessRoles roles, IEmployeeDirectory employees, IUserDirectory users) : ICareerModel
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedTrackMatrix>> PublishedTrackMatricesAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken = default)
    {
        var org = await employees.FindOrgSnapshotAsync(cancellationToken);
        var ancestryOf = unitIds.Distinct().ToDictionary(unitId => unitId, unitId => org.Ancestry(unitId));
        var ancestorIds = ancestryOf.Values.SelectMany(ancestry => ancestry).Select(unit => unit.Id).Distinct().ToArray();
        var versions = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .Where(version => version.Status == MatrixVersionStatus.Published && version.Matrix.TrackId != null && ancestorIds.Contains(version.Matrix.OrgUnitId))
            .ToListAsync(cancellationToken);
        return [.. ancestryOf
            .SelectMany(pair => MatrixInheritance.InEffect(pair.Value, versions).Select(version => (UnitId: pair.Key, Version: version)))
            .Select(pair => pair.Version.Snapshot is { Track: { } track } snapshot
                ? new PublishedTrackMatrix(pair.UnitId, pair.Version.MatrixId, pair.Version.Id, pair.Version.Number, track.Id, track.Name, LevelsOf(pair.Version, snapshot))
                : null)
            .OfType<PublishedTrackMatrix>()
            .OrderBy(matrix => matrix.TrackName, TextCollation.Comparer)
            .ThenBy(matrix => matrix.MatrixId)
            .ThenBy(matrix => matrix.UnitId)];
    }

    /// <inheritdoc />
    public async Task<CareerVersion?> VersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await context.Set<MatrixVersion>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == versionId
                    && (candidate.Status == MatrixVersionStatus.Published || candidate.Status == MatrixVersionStatus.Archived)
                    && candidate.Matrix.TrackId != null,
                cancellationToken);
        if (version?.Snapshot is not { } snapshot)
        {
            return null;
        }

        var counted = version.Content.CompetenciesWith(snapshot.Compositions());
        var competencies = snapshot.Competencies.ToDictionary(competency => competency.Id);
        var scales = snapshot.Scales.ToDictionary(scale => scale.Id);
        var countedIds = counted.Select(item => item.CompetencyId).ToHashSet();
        var blockEntries = snapshot.SharedBlocks.ToDictionary(block => block.Id);
        return new CareerVersion(
            LevelsOf(version, snapshot),
            [.. counted
                .Where(item => competencies.ContainsKey(item.CompetencyId))
                .Select(item => CompetencyOf(item, competencies[item.CompetencyId], scales))],
            [.. version.Content.Requirements
                .Where(requirement => countedIds.Contains(requirement.CompetencyId))
                .Select(requirement => new CareerVersionRequirement(requirement.LevelId, requirement.CompetencyId, requirement.RequiredScore))],
            [.. version.Content.BlockWeights.Select(weight => new CareerVersionBlockWeight(weight.LevelId, weight.BlockId, weight.Percent))],
            [.. version.Content.SharedBlocks
                .Where(block => blockEntries.ContainsKey(block.BlockId) && counted.Any(item => item.BlockId == block.BlockId))
                .Select((block, order) => new CareerVersionBlock(block.BlockId, blockEntries[block.BlockId].Name, order))]);
    }

    /// <inheritdoc />
    public CareerScore Score(CareerVersion version, Guid levelId, IReadOnlyDictionary<Guid, decimal> scores)
    {
        var level = version.Levels.FirstOrDefault(item => item.Id == levelId)
            ?? throw new ArgumentException($"Level {levelId} is not a level of the version", nameof(levelId));
        var required = version.Requirements.Where(requirement => requirement.LevelId == levelId).ToDictionary(requirement => requirement.CompetencyId, requirement => requirement.RequiredScore);
        var counted = version.Competencies.Where(competency => required.ContainsKey(competency.Id)).ToList();
        var standings = counted
            .Select(competency =>
            {
                decimal? given = scores.TryGetValue(competency.Id, out var score) ? score : null;
                return new CareerCompetencyScore(competency.Id, required[competency.Id], given, given >= required[competency.Id]);
            })
            .ToList();
        var rating = counted
            .GroupBy(competency => competency.BlockId)
            .Sum(group => WeightOf(version, levelId, group.Key) * group.Average(competency => ShareOf(competency, scores)));
        var isCriticalMet = counted.Zip(standings).All(pair => pair.First.Type != CompetencyType.Critical || pair.Second.IsMet);
        return new CareerScore(rating, level.MinOverallPercent, rating >= (level.MinOverallPercent ?? 0) && isCriticalMet, standings);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> AllowedTargetLevelsAsync(
        Guid positionId,
        IReadOnlyCollection<PublishedTrackMatrix> matrices,
        CancellationToken cancellationToken = default)
    {
        var trackIds = matrices.Select(matrix => matrix.TrackId).Distinct().ToList();
        var links = context.Set<CareerLevelPosition>();
        var currentByTrack = await context.Set<CareerLevel>()
            .AsNoTracking()
            .Where(level => trackIds.Contains(level.TrackId)
                && level.Status == CatalogStatus.Active
                && level.Track.Status == CatalogStatus.Active
                && links.Any(link => link.LevelId == level.Id && link.PositionId == positionId))
            .ToDictionaryAsync(level => level.TrackId, level => level.Position, cancellationToken);
        var levelIds = matrices.SelectMany(matrix => matrix.Levels).Select(level => level.Id).Distinct().ToList();
        var positionOf = await context.Set<CareerLevel>()
            .AsNoTracking()
            .Where(level => levelIds.Contains(level.Id))
            .ToDictionaryAsync(level => level.Id, level => level.Position, cancellationToken);
        return matrices.DistinctBy(matrix => matrix.MatrixId).ToDictionary(
            matrix => matrix.MatrixId,
            matrix => currentByTrack.TryGetValue(matrix.TrackId, out var current)
                ? NextLevel(matrix, current, positionOf)
                : (IReadOnlyList<Guid>)[.. matrix.Levels.Select(level => level.Id)]);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OrgUnitStatus>> ResponsibleUnitsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        roles.HrUnitsAsync(userId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ResponsibleHr>> ResponsibleHrAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken = default)
    {
        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        var assigneeOf = await roles.AllAssigneesAsync(cancellationToken);
        var accounts = await users.FindByEmployeeIdsAsync([.. assigneeOf.Values.Distinct()], cancellationToken);
        var employeeOf = accounts.Values.ToDictionary(account => account.Id, account => account.EmployeeId!.Value);
        return unitIds
            .Distinct()
            .Select(unitId => (UnitId: unitId, UserId: ProcessRoles.ResponsibleHr(snapshot.Ancestry(unitId), assigneeOf, accounts)))
            .Where(pair => pair.UserId is not null)
            .ToDictionary(pair => pair.UnitId, pair => new ResponsibleHr(employeeOf[pair.UserId!.Value], pair.UserId!.Value));
    }

    private static IReadOnlyList<Guid> NextLevel(PublishedTrackMatrix matrix, int currentPosition, IReadOnlyDictionary<Guid, int> positionOf) =>
        [.. matrix.Levels
            .Where(level => positionOf.TryGetValue(level.Id, out var position) && position > currentPosition)
            .OrderBy(level => positionOf[level.Id])
            .Take(1)
            .Select(level => level.Id)];

    private static List<CareerVersionLevel> LevelsOf(MatrixVersion version, MatrixEntries snapshot)
    {
        var columns = version.Content.Levels.ToDictionary(level => level.LevelId);
        return [.. snapshot.Levels
            .Where(level => columns.ContainsKey(level.Id))
            .Select((level, order) => new CareerVersionLevel(level.Id, level.Name, order, columns[level.Id].MinOverallPercent, columns[level.Id].RequiredEducation))];
    }

    private static CareerVersionCompetency CompetencyOf(MatrixCompetency item, MatrixCompetencyEntry entry, IReadOnlyDictionary<Guid, MatrixScaleEntry> scales)
    {
        IReadOnlyList<ScalePoint> points = entry.ScaleId is { } scaleId && scales.TryGetValue(scaleId, out var scale) ? scale.Points : [];
        return new CareerVersionCompetency(
            entry.Id,
            entry.Name,
            entry.Description,
            item.BlockId,
            item.Type ?? CompetencyType.Additional,
            points.Count == 0 ? 0 : points.Max(point => point.Score),
            points);
    }

    private static decimal WeightOf(CareerVersion version, Guid levelId, Guid? blockId) =>
        version.BlockWeights.FirstOrDefault(weight => weight.LevelId == levelId && weight.BlockId == blockId)?.Percent ?? 0;

    private static decimal ShareOf(CareerVersionCompetency competency, IReadOnlyDictionary<Guid, decimal> scores) =>
        competency.MaxScore > 0 && scores.TryGetValue(competency.Id, out var score) ? score / competency.MaxScore : 0;
}
