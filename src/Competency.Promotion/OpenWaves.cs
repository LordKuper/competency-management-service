using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Reads the waves everybody sees: those that have not started and those taking applications, with what an application can aim at. Before
/// a wave binds its matrix versions the aims are the published track matrices of its units; afterwards they are the bound versions, whatever
/// is published later. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class OpenWaves(
    AppDbContext context,
    IEmployeeDirectory employees,
    ICareerModel career,
    PromotionCalendar calendar)
{
    private const int FirstStage = 1;

    /// <summary>
    /// Reads the open waves.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The waves by start date; empty when none is open.</returns>
    public async Task<IReadOnlyList<OpenWaveResponse>> ReadAsync(CancellationToken cancellationToken)
    {
        var waves = (await context.Set<PromotionWave>().AsNoTracking().Where(wave => wave.ProcessedStage < PromotionWave.StageCount).ToListAsync(cancellationToken))
            .Where(wave => calendar.StageOf(wave) <= FirstStage)
            .OrderBy(wave => wave.StartsOn)
            .ThenBy(wave => wave.Id)
            .ToList();
        if (waves.Count == 0)
        {
            return [];
        }

        var waveIds = waves.Select(wave => wave.Id).ToArray();
        var members = await context.Set<PromotionWaveUnit>().AsNoTracking().Where(member => waveIds.Contains(member.WaveId)).ToListAsync(cancellationToken);
        var units = await employees.FindUnitsAsync([.. members.Select(member => member.OrgUnitId).Distinct()], cancellationToken);
        var bound = await context.Set<PromotionWaveMatrix>().AsNoTracking().Where(matrix => waveIds.Contains(matrix.WaveId)).ToListAsync(cancellationToken);
        var boundWaves = bound.Select(matrix => matrix.WaveId).ToHashSet();
        var published = await career.PublishedTrackMatricesAsync(
            [.. members.Where(member => !boundWaves.Contains(member.WaveId)).Select(member => member.OrgUnitId).Distinct()],
            cancellationToken);
        var levelsOfVersion = await LevelsAsync(bound, cancellationToken);

        return [.. waves.Select(wave => new OpenWaveResponse(
            wave.Id,
            wave.StartsOn,
            wave.EndsOn,
            wave.ResultsPublishedOn,
            calendar.StageOf(wave),
            [.. members
                .Where(member => member.WaveId == wave.Id)
                .Select(member => units[member.OrgUnitId])
                .OrderBy(unit => unit.Name, TextCollation.Comparer)
                .ThenBy(unit => unit.Id)
                .Select(unit => new OpenWaveUnitResponse(
                    unit.Id,
                    unit.Name,
                    boundWaves.Contains(wave.Id)
                        ? TargetsOfBound(bound.Where(matrix => matrix.WaveId == wave.Id && matrix.OrgUnitId == unit.Id), levelsOfVersion)
                        : [.. published.Where(matrix => matrix.UnitId == unit.Id).Select(TargetOf)]))]))];
    }

    private async Task<Dictionary<Guid, IReadOnlyList<WaveTargetLevelResponse>>> LevelsAsync(
        IReadOnlyCollection<PromotionWaveMatrix> bound,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, IReadOnlyList<WaveTargetLevelResponse>> levels = [];
        foreach (var versionId in bound.Select(matrix => matrix.VersionId).Distinct())
        {
            if (await career.VersionAsync(versionId, cancellationToken) is { } version)
            {
                levels[versionId] = [.. version.Levels.OrderBy(level => level.Order).Select(level => new WaveTargetLevelResponse(level.Id, level.Name))];
            }
        }

        return levels;
    }

    private static List<WaveTargetResponse> TargetsOfBound(
        IEnumerable<PromotionWaveMatrix> matrices,
        IReadOnlyDictionary<Guid, IReadOnlyList<WaveTargetLevelResponse>> levelsOfVersion) =>
        [.. matrices
            .Where(matrix => levelsOfVersion.ContainsKey(matrix.VersionId))
            .OrderBy(matrix => matrix.TrackName, TextCollation.Comparer)
            .ThenBy(matrix => matrix.MatrixId)
            .Select(matrix => new WaveTargetResponse(matrix.MatrixId, matrix.TrackId, matrix.TrackName, levelsOfVersion[matrix.VersionId]))];

    private static WaveTargetResponse TargetOf(PublishedTrackMatrix matrix) =>
        new(
            matrix.MatrixId,
            matrix.TrackId,
            matrix.TrackName,
            [.. matrix.Levels.OrderBy(level => level.Order).Select(level => new WaveTargetLevelResponse(level.Id, level.Name))]);
}
