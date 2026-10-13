using System.Globalization;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The catalogue side of matrix versions: which entries a version may refer to, whether a version is complete enough to be handed on,
/// and the entries it refers to as they stand. A version refers to the levels of its matrix track, to competencies of its scale and of
/// the matrix direction or general ones, and to entries that are active or drafts whose author heads the matrix unit. Draft levels of
/// the matrix track are usable whoever wrote them, because a matrix may be built on a draft track of its unit, which its HR edits too.
/// </summary>
internal sealed class MatrixCatalog(AppDbContext context, IEmployeeDirectory employees, ProcessRoles roles, CatalogStore store)
{
    private const string ContentField = "content";
    private const string ForeignLevelMessage = "Уровень не из трека матрицы";
    private const string ForeignCompetencyMessage = "Компетенция не на шкале версии или не из направления матрицы";

    private static readonly NumberFormatInfo DecimalComma = new() { NumberDecimalSeparator = "," };

    /// <summary>
    /// Says why the edited content cannot refer to its entries. Only entries the edit adds must be usable, so an entry deactivated
    /// since may stay until the version is handed on, when <see cref="CompletenessProblemsAsync"/> reports it.
    /// </summary>
    /// <param name="version">The version being edited, with its current content.</param>
    /// <param name="content">The new content, already valid on its own.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The problem by field name; null when the content may be saved.</returns>
    public async Task<Dictionary<string, string[]>?> ReferenceProblemAsync(MatrixVersion version, MatrixContent content, CancellationToken cancellationToken)
    {
        var entries = await LoadAsync(version.ScaleId, content, cancellationToken);
        var headId = await HeadOfAsync(version.Matrix, cancellationToken);
        var levels = content.Levels.Select(item => entries.Levels.GetValueOrDefault(item.LevelId)).ToList();
        var competencies = content.Competencies.Select(item => entries.Competencies.GetValueOrDefault(item.CompetencyId)).ToList();
        var previousIds = version.Content.Levels.Select(level => level.LevelId)
            .Concat(version.Content.Competencies.Select(competency => competency.CompetencyId))
            .ToHashSet();
        IEnumerable<CatalogEntry?> referenced = [.. levels, .. competencies];

        string? problem = null;
        if (levels.Any(level => level is null || level.TrackId != version.Matrix.TrackId))
        {
            problem = ForeignLevelMessage;
        }
        else if (competencies.Any(competency => competency is null || !Fits(competency, version)))
        {
            problem = ForeignCompetencyMessage;
        }
        else if (referenced.Any(entry => !previousIds.Contains(entry!.Id) && !IsUsable(entry, headId)))
        {
            problem = CatalogEndpoints.UnavailableMessage;
        }
        else if (content.Requirements.Any(requirement => !entries.HasScore(requirement.RequiredScore)))
        {
            problem = "Требуемого уровня нет в шкале версии";
        }

        return problem is null ? null : new Dictionary<string, string[]> { [ContentField] = [problem] };
    }

    /// <summary>
    /// Says what keeps the version from being handed on: a level without requirements, block coefficients of a level that do not add
    /// up to 100 %, a critical requirement without a required score, a level of the matrix track without a linked active position,
    /// an entry that is no longer usable or no longer fits the version, or a required score its scale no longer has.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The problems; empty when the version is complete.</returns>
    public async Task<List<MatrixProblem>> CompletenessProblemsAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        var content = version.Content;
        if (content.Levels.Count == 0)
        {
            return [new MatrixProblem(MatrixProblemCode.NoLevels, "В версии нет уровней")];
        }

        var entries = await LoadAsync(version.ScaleId, content, cancellationToken);
        var problems = new List<MatrixProblem>();
        foreach (var level in content.Levels)
        {
            var levelId = level.LevelId;
            var name = entries.Levels.GetValueOrDefault(levelId)?.Name ?? levelId.ToString();
            if (!content.Requirements.Any(requirement => requirement.LevelId == levelId))
            {
                problems.Add(new MatrixProblem(MatrixProblemCode.LevelWithoutRequirements, $"У уровня «{name}» нет требований", levelId));
            }

            var sum = content.BlockWeights.Where(weight => weight.LevelId == levelId).Sum(weight => weight.Percent);
            if (sum != MatrixContent.MaxPercent)
            {
                problems.Add(new MatrixProblem(
                    MatrixProblemCode.BlockWeights,
                    $"Коэффициенты блоков уровня «{name}» дают в сумме {sum.ToString("0.##", DecimalComma)} %, нужно 100 %",
                    levelId));
            }
        }

        foreach (var requirement in content.Requirements.Where(requirement => requirement.Type == CompetencyType.Critical && requirement.RequiredScore is null)
                     .DistinctBy(requirement => requirement.CompetencyId))
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.CriticalWithoutScore,
                $"У критической компетенции «{entries.CompetencyName(requirement.CompetencyId)}» не задан минимальный уровень",
                CompetencyId: requirement.CompetencyId));
        }

        var readiness = await store.ReadinessAsync(version.Matrix.TrackId, cancellationToken);
        problems.AddRange(readiness.LevelsWithoutActivePosition.Select(level => new MatrixProblem(
            MatrixProblemCode.LevelWithoutActivePosition,
            $"У уровня «{level.Name}» трека «{level.Track.Name}» нет связанной действующей должности",
            level.Id)));
        problems.AddRange(EntryProblems(version, entries, await HeadOfAsync(version.Matrix, cancellationToken)));
        foreach (var requirement in content.Requirements.Where(requirement => !entries.HasScore(requirement.RequiredScore)))
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.ScoreOutOfScale,
                $"Требуемого уровня {requirement.RequiredScore} компетенции «{entries.CompetencyName(requirement.CompetencyId)}» нет в шкале версии",
                requirement.LevelId,
                requirement.CompetencyId));
        }

        return problems;
    }

    /// <summary>
    /// The entries the version refers to as they stand now: levels in track order, competencies in the order of the content.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The entries.</returns>
    public async Task<MatrixEntries> EntriesAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        var matrix = version.Matrix;
        var content = version.Content;
        var entries = await LoadAsync(version.ScaleId, content, cancellationToken);
        var direction = await context.Set<CareerDirection>().AsNoTracking().FirstAsync(entry => entry.Id == matrix.DirectionId, cancellationToken);
        var track = await context.Set<CareerTrack>().AsNoTracking().FirstAsync(entry => entry.Id == matrix.TrackId, cancellationToken);
        var categoryIds = entries.Competencies.Values.Select(competency => competency.CategoryId).Distinct().ToList();
        var categoryNames = await context.Set<CompetencyCategory>()
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);

        return new MatrixEntries(
            new MatrixNamedEntry(direction.Id, direction.Name, direction.Status),
            new MatrixNamedEntry(track.Id, track.Name, track.Status),
            new MatrixScaleEntry(entries.Scale.Id, entries.Scale.Name, entries.Scale.Points, entries.Scale.Status),
            [.. entries.Levels.Values
                .OrderBy(level => level.Position)
                .Select(level => new MatrixLevelEntry(level.Id, level.Name, level.Status))],
            [.. content.Competencies
                .Select(item => entries.Competencies.GetValueOrDefault(item.CompetencyId))
                .OfType<CareerCompetency>()
                .Select(competency => new MatrixCompetencyEntry(
                    competency.Id,
                    competency.Name,
                    competency.Description,
                    competency.CategoryId,
                    categoryNames.GetValueOrDefault(competency.CategoryId, string.Empty),
                    competency.DirectionId,
                    competency.Type,
                    CompetencyVersion.NumberOf(competency),
                    competency.Status))]);
    }

    private static IEnumerable<MatrixProblem> EntryProblems(MatrixVersion version, LoadedEntries entries, Guid? headId)
    {
        IEnumerable<CatalogEntry> all = [entries.Scale, .. entries.Levels.Values, .. entries.Competencies.Values];
        foreach (var entry in all.Where(entry => !IsUsable(entry, headId)))
        {
            yield return new MatrixProblem(
                MatrixProblemCode.InactiveEntry,
                $"В версии есть неактивная запись «{entry.Name}»",
                entry is CareerLevel ? entry.Id : null,
                entry is CareerCompetency ? entry.Id : null);
        }

        foreach (var competency in entries.Competencies.Values.Where(competency => !Fits(competency, version)))
        {
            yield return new MatrixProblem(MatrixProblemCode.ForeignCompetency, $"{ForeignCompetencyMessage}: «{competency.Name}»", CompetencyId: competency.Id);
        }
    }

    private static bool IsUsable(CatalogEntry entry, Guid? headId) =>
        entry.Status == CatalogStatus.Active
        || (entry.Status == CatalogStatus.Draft && (entry is CareerLevel || (headId is not null && entry.AuthorId == headId)));

    /// <summary>
    /// The account of the matrix unit's head, whose drafts the matrix may use; null when the unit has no head who can act.
    /// </summary>
    private async Task<Guid?> HeadOfAsync(CompetencyMatrix matrix, CancellationToken cancellationToken) =>
        await employees.FindUnitAsync(matrix.OrgUnitId, cancellationToken) is { } unit
            ? await roles.HeadAccountAsync(unit, cancellationToken)
            : null;

    private static bool Fits(CareerCompetency competency, MatrixVersion version) =>
        competency.ScaleId == version.ScaleId && (competency.DirectionId is null || competency.DirectionId == version.Matrix.DirectionId);

    private async Task<LoadedEntries> LoadAsync(Guid scaleId, MatrixContent content, CancellationToken cancellationToken)
    {
        var levelIds = content.Levels.Select(level => level.LevelId).ToList();
        var competencyIds = content.Competencies.Select(competency => competency.CompetencyId).ToList();
        return new LoadedEntries(
            await context.Set<CompetencyScale>().AsNoTracking().FirstAsync(scale => scale.Id == scaleId, cancellationToken),
            await context.Set<CareerLevel>().AsNoTracking().Where(level => levelIds.Contains(level.Id)).ToDictionaryAsync(level => level.Id, cancellationToken),
            await context.Set<CareerCompetency>().AsNoTracking().Where(competency => competencyIds.Contains(competency.Id)).ToDictionaryAsync(competency => competency.Id, cancellationToken));
    }

    private sealed record LoadedEntries(
        CompetencyScale Scale,
        Dictionary<Guid, CareerLevel> Levels,
        Dictionary<Guid, CareerCompetency> Competencies)
    {
        public bool HasScore(int? score) => score is null || Scale.Points.Any(point => point.Score == score);

        public string CompetencyName(Guid competencyId) => Competencies.GetValueOrDefault(competencyId)?.Name ?? competencyId.ToString();
    }
}
