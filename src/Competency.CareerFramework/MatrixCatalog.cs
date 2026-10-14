using System.Globalization;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The catalogue side of matrix versions: which entries a version may refer to, whether a version is complete enough to be handed on,
/// and the entries it refers to as they stand. A track matrix version refers to the levels of its track; a position matrix version has
/// one column, its position. A version refers to shared blocks and to competencies of its scale, for a track matrix of the matrix
/// direction or general ones, and to entries that are active or drafts whose author heads the matrix unit. Draft levels of the matrix
/// track are usable whoever wrote them, because a matrix may be built on a draft track of its unit, which its HR edits too. The
/// competencies of a version include the compositions of its shared blocks, which keep their own scales; a requirement of a competency
/// that has left its block counts nowhere.
/// </summary>
internal sealed class MatrixCatalog(AppDbContext context, IEmployeeDirectory employees, ProcessRoles roles, CatalogStore store)
{
    private const string ContentField = "content";
    private const string ForeignLevelMessage = "Уровень не из трека матрицы";
    private const string PositionColumnMessage = "У матрицы должности один столбец требований — её должность";
    private const string ForeignCompetencyMessage = "Компетенция не на шкале версии или не из области матрицы";
    private const string ForeignRequirementMessage = "Требование относится к компетенции не из версии";

    private static readonly NumberFormatInfo DecimalComma = new() { NumberDecimalSeparator = "," };

    /// <summary>
    /// Says why the edited content cannot refer to its entries. Only entries and requirements the edit adds must be usable, so an entry
    /// deactivated since, or a requirement of a competency that has left its block, may stay until the version is handed on, when
    /// <see cref="CompletenessProblemsAsync"/> reports what matters.
    /// </summary>
    /// <param name="version">The version being edited, with its current content.</param>
    /// <param name="content">The new content, already valid on its own.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The problem by field name; null when the content may be saved.</returns>
    public async Task<Dictionary<string, string[]>?> ReferenceProblemAsync(MatrixVersion version, MatrixContent content, CancellationToken cancellationToken)
    {
        var entries = await LoadAsync(version, content, cancellationToken);
        var matrix = version.Matrix;
        var headId = await HeadOfAsync(matrix, cancellationToken);
        var previous = version.Content;
        var previousIds = previous.Levels.Select(level => level.LevelId)
            .Concat(previous.Competencies.Select(competency => competency.CompetencyId))
            .Concat(previous.SharedBlocks.Select(block => block.BlockId))
            .ToHashSet();
        var previousRequirements = previous.Requirements.Select(requirement => (requirement.LevelId, requirement.CompetencyId)).ToHashSet();
        var levels = matrix.TrackId is null ? [] : content.Levels.Select(item => entries.Levels.GetValueOrDefault(item.LevelId)).ToList();
        var competencies = content.Competencies.Select(item => entries.Competencies.GetValueOrDefault(item.CompetencyId)).ToList();
        var blocks = content.SharedBlocks.Select(item => entries.Blocks.GetValueOrDefault(item.BlockId)).ToList();
        IEnumerable<CatalogEntry?> referenced = [.. levels, .. competencies, .. blocks];

        string? problem = null;
        if (LevelProblem(matrix, content, entries) is { } levelProblem)
        {
            problem = levelProblem;
        }
        else if (competencies.Any(competency => competency is null || !IsOnScale(competency, version) || !IsOfDirection(competency, matrix)))
        {
            problem = ForeignCompetencyMessage;
        }
        else if (referenced.Any(entry => entry is null || (!previousIds.Contains(entry.Id) && !IsUsable(entry, headId))))
        {
            problem = CatalogEndpoints.UnavailableMessage;
        }
        else if (content.Requirements.Any(requirement => !entries.Includes(requirement.CompetencyId)
                     && !previousRequirements.Contains((requirement.LevelId, requirement.CompetencyId))))
        {
            problem = ForeignRequirementMessage;
        }
        else if (entries.Counted(content.Requirements).FirstOrDefault(requirement => !entries.HasScore(requirement)) is { } outOfScale)
        {
            problem = $"Требуемого уровня нет в {entries.ScaleWords(outOfScale.CompetencyId)}";
        }

        return problem is null ? null : new Dictionary<string, string[]> { [ContentField] = [problem] };
    }

    /// <summary>
    /// Says what keeps the version from being handed on. For a track matrix: a level without requirements, block coefficients of a level
    /// that do not add up to 100 %, a level of the matrix track without a linked active position. For a position matrix: no competencies,
    /// a competency without a required score, coefficients that do not add up to 100 %, a position no longer available to the matrix
    /// unit. For both: a competency of a shared block or outside blocks without requirements, a competency in two groups, a critical
    /// requirement without a required score, an entry that is no longer usable or no longer fits the version, or a required score the
    /// competency's scale no longer has.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The problems; empty when the version is complete.</returns>
    public async Task<List<MatrixProblem>> CompletenessProblemsAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        var content = version.Content;
        var matrix = version.Matrix;
        if (matrix.PositionId is null && content.Levels.Count == 0)
        {
            return [new MatrixProblem(MatrixProblemCode.NoLevels, "В версии нет уровней")];
        }

        var entries = await LoadAsync(version, content, cancellationToken);
        var requirements = entries.Counted(content.Requirements).ToList();
        var problems = matrix.PositionId is { } positionId
            ? await PositionProblemsAsync(matrix, positionId, content, entries, requirements, cancellationToken)
            : await TrackProblemsAsync(matrix, content, entries, requirements, cancellationToken);

        foreach (var requirement in requirements.Where(requirement => requirement.Type == CompetencyType.Critical && requirement.RequiredScore is null)
                     .DistinctBy(requirement => requirement.CompetencyId))
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.CriticalWithoutScore,
                $"У критической компетенции «{entries.CompetencyName(requirement.CompetencyId)}» не задан минимальный уровень",
                CompetencyId: requirement.CompetencyId));
        }

        foreach (var competencyId in entries.Set.GroupBy(item => item.CompetencyId).Where(group => group.Count() > 1).Select(group => group.Key))
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.DuplicateCompetency,
                $"Компетенция «{entries.CompetencyName(competencyId)}» входит в версию дважды",
                CompetencyId: competencyId));
        }

        problems.AddRange(EntryProblems(version, entries, await HeadOfAsync(matrix, cancellationToken)));
        foreach (var requirement in requirements.Where(requirement => !entries.HasScore(requirement)))
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.ScoreOutOfScale,
                $"Требуемого уровня {requirement.RequiredScore} компетенции «{entries.CompetencyName(requirement.CompetencyId)}» нет в {entries.ScaleWords(requirement.CompetencyId)}",
                requirement.LevelId,
                requirement.CompetencyId));
        }

        return problems;
    }

    /// <summary>
    /// The entries the version refers to as they stand now: levels in track order, or the position; competencies in the order of the
    /// content, then those of the shared blocks; the shared blocks with their scales and compositions.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The entries.</returns>
    public async Task<MatrixEntries> EntriesAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        var matrix = version.Matrix;
        var entries = await LoadAsync(version, version.Content, cancellationToken);
        var direction = matrix.DirectionId is { } directionId
            ? await context.Set<CareerDirection>().AsNoTracking().FirstAsync(entry => entry.Id == directionId, cancellationToken)
            : null;
        var track = matrix.TrackId is { } trackId
            ? await context.Set<CareerTrack>().AsNoTracking().FirstAsync(entry => entry.Id == trackId, cancellationToken)
            : null;
        var position = matrix.PositionId is { } positionId ? await PositionAsync(positionId, cancellationToken) : null;
        var categoryIds = entries.Competencies.Values.Select(competency => competency.CategoryId).Distinct().ToList();
        var categoryNames = await context.Set<CompetencyCategory>()
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);

        return new MatrixEntries(
            direction is null ? null : new MatrixNamedEntry(direction.Id, direction.Name, direction.Status),
            track is null ? null : new MatrixNamedEntry(track.Id, track.Name, track.Status),
            ScaleEntryOf(entries.Scale),
            [.. entries.Levels.Values
                .OrderBy(level => level.Position)
                .Select(level => new MatrixLevelEntry(level.Id, level.Name, level.Status))],
            [.. entries.Set
                .Select(item => item.CompetencyId)
                .Distinct()
                .Select(entries.Competencies.GetValueOrDefault)
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
                    competency.Status))],
            position is null ? null : new MatrixNamedEntry(position.Id, position.Name, position.IsActive ? CatalogStatus.Active : CatalogStatus.Inactive),
            [.. version.Content.SharedBlocks
                .Select(item => entries.Blocks.GetValueOrDefault(item.BlockId))
                .OfType<CompetencyBlock>()
                .Select(block => new MatrixSharedBlockEntry(block.Id, block.Name, block.Status, ScaleEntryOf(entries.Scales[block.ScaleId]), block.CompetencyIds))]);
    }

    private static MatrixScaleEntry ScaleEntryOf(CompetencyScale scale) => new(scale.Id, scale.Name, scale.Points, scale.Status);

    private static string? LevelProblem(CompetencyMatrix matrix, MatrixContent content, LoadedEntries entries) => matrix.PositionId is { } positionId
        ? content.Levels is [{ } column] && column.LevelId == positionId ? null : PositionColumnMessage
        : content.Levels.Any(item => entries.Levels.GetValueOrDefault(item.LevelId)?.TrackId != matrix.TrackId) ? ForeignLevelMessage : null;

    private static IEnumerable<MatrixProblem> EntryProblems(MatrixVersion version, LoadedEntries entries, Guid? headId)
    {
        IEnumerable<CatalogEntry> all = [entries.Scale, .. entries.Levels.Values, .. entries.Competencies.Values, .. entries.Blocks.Values];
        foreach (var entry in all.Where(entry => !IsUsable(entry, headId)))
        {
            yield return new MatrixProblem(
                MatrixProblemCode.InactiveEntry,
                $"В версии есть неактивная запись «{entry.Name}»",
                entry is CareerLevel ? entry.Id : null,
                entry is CareerCompetency ? entry.Id : null);
        }

        foreach (var (competency, isOwn) in entries.Competencies.Values.Select(competency => (competency, entries.IsOnVersionScale(competency.Id))))
        {
            if (isOwn && !IsOnScale(competency, version))
            {
                yield return new MatrixProblem(MatrixProblemCode.ForeignCompetency, $"Компетенция «{competency.Name}» на другой шкале, чем версия", CompetencyId: competency.Id);
            }
            else if (!IsOfDirection(competency, version.Matrix))
            {
                yield return new MatrixProblem(MatrixProblemCode.ForeignCompetency, $"Компетенция «{competency.Name}» не из области матрицы", CompetencyId: competency.Id);
            }
        }
    }

    private static bool IsUsable(CatalogEntry entry, Guid? headId) =>
        entry.Status == CatalogStatus.Active
        || (entry.Status == CatalogStatus.Draft && (entry is CareerLevel || (headId is not null && entry.AuthorId == headId)));

    private static bool IsOnScale(CareerCompetency competency, MatrixVersion version) => competency.ScaleId == version.ScaleId;

    private static bool IsOfDirection(CareerCompetency competency, CompetencyMatrix matrix) =>
        matrix.DirectionId is null || competency.DirectionId is null || competency.DirectionId == matrix.DirectionId;

    /// <summary>
    /// What keeps a track matrix version from being handed on by its levels and groups: levels without requirements, coefficients of a
    /// level not adding up to 100 %, competencies of shared blocks and outside blocks without requirements, levels without an active position.
    /// </summary>
    private async Task<List<MatrixProblem>> TrackProblemsAsync(
        CompetencyMatrix matrix,
        MatrixContent content,
        LoadedEntries entries,
        IReadOnlyList<MatrixRequirement> requirements,
        CancellationToken cancellationToken)
    {
        var problems = new List<MatrixProblem>();
        foreach (var level in content.Levels)
        {
            var levelId = level.LevelId;
            var name = entries.Levels.GetValueOrDefault(levelId)?.Name ?? levelId.ToString();
            if (!requirements.Any(requirement => requirement.LevelId == levelId))
            {
                problems.Add(new MatrixProblem(MatrixProblemCode.LevelWithoutRequirements, $"У уровня «{name}» нет требований", levelId));
            }

            var sum = WeightSum(content, levelId);
            if (sum != MatrixContent.MaxPercent)
            {
                problems.Add(new MatrixProblem(
                    MatrixProblemCode.BlockWeights,
                    $"Коэффициенты блоков уровня «{name}» дают в сумме {sum.ToString("0.##", DecimalComma)} %, нужно 100 %",
                    levelId));
            }
        }

        var ownBlockIds = content.Blocks.Select(block => block.Id).ToHashSet();
        foreach (var competencyId in entries.Set
                     .Where(item => item.BlockId is not { } blockId || !ownBlockIds.Contains(blockId))
                     .Select(item => item.CompetencyId)
                     .Distinct()
                     .Where(competencyId => !requirements.Any(requirement => requirement.CompetencyId == competencyId)))
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.CompetencyWithoutRequirements,
                $"У компетенции «{entries.CompetencyName(competencyId)}» нет требований",
                CompetencyId: competencyId));
        }

        var readiness = await store.ReadinessAsync(matrix.TrackId!.Value, cancellationToken);
        problems.AddRange(readiness.LevelsWithoutActivePosition.Select(level => new MatrixProblem(
            MatrixProblemCode.LevelWithoutActivePosition,
            $"У уровня «{level.Name}» трека «{level.Track.Name}» нет связанной действующей должности",
            level.Id)));
        return problems;
    }

    /// <summary>
    /// What keeps a position matrix version from being handed on by its one column and its position: no competencies, coefficients not
    /// adding up to 100 %, competencies without a required score, and a position that is inactive or no longer on the lists of the matrix
    /// unit and the units above it.
    /// </summary>
    private async Task<List<MatrixProblem>> PositionProblemsAsync(
        CompetencyMatrix matrix,
        Guid positionId,
        MatrixContent content,
        LoadedEntries entries,
        IReadOnlyList<MatrixRequirement> requirements,
        CancellationToken cancellationToken)
    {
        var problems = new List<MatrixProblem>();
        var competencyIds = entries.Set.Select(item => item.CompetencyId).Distinct().ToList();
        if (competencyIds.Count == 0)
        {
            problems.Add(new MatrixProblem(MatrixProblemCode.NoCompetencies, "В матрице должности нет компетенций"));
        }

        var sum = WeightSum(content, positionId);
        if (sum != MatrixContent.MaxPercent)
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.BlockWeights,
                $"Коэффициенты блоков дают в сумме {sum.ToString("0.##", DecimalComma)} %, нужно 100 %",
                positionId));
        }

        foreach (var competencyId in competencyIds.Where(competencyId =>
                     !requirements.Any(requirement => requirement.CompetencyId == competencyId && requirement.RequiredScore is not null)))
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.CompetencyWithoutScore,
                $"У компетенции «{entries.CompetencyName(competencyId)}» не задан требуемый балл",
                CompetencyId: competencyId));
        }

        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        var position = snapshot.Positions.GetValueOrDefault(positionId);
        var name = position?.Name ?? positionId.ToString();
        if (position is not { IsActive: true })
        {
            problems.Add(new MatrixProblem(MatrixProblemCode.InactiveEntry, $"В версии есть неактивная запись «{name}»", positionId));
        }
        else if (!snapshot.AvailablePositionIds(matrix.OrgUnitId).Contains(positionId))
        {
            problems.Add(new MatrixProblem(MatrixProblemCode.PositionUnavailable, $"Должность «{name}» недоступна подразделению матрицы", positionId));
        }

        return problems;
    }

    private static decimal WeightSum(MatrixContent content, Guid levelId) =>
        content.BlockWeights.Where(weight => weight.LevelId == levelId).Sum(weight => weight.Percent);

    /// <summary>
    /// The account of the matrix unit's head, whose drafts the matrix may use; null when the unit has no head who can act.
    /// </summary>
    private async Task<Guid?> HeadOfAsync(CompetencyMatrix matrix, CancellationToken cancellationToken) =>
        await employees.FindUnitAsync(matrix.OrgUnitId, cancellationToken) is { } unit
            ? await roles.HeadAccountAsync(unit, cancellationToken)
            : null;

    private async Task<PositionStatus?> PositionAsync(Guid positionId, CancellationToken cancellationToken) =>
        (await employees.FindOrgSnapshotAsync(cancellationToken)).Positions.GetValueOrDefault(positionId);

    /// <summary>
    /// Reads what the content refers to: the version's scale and the scales of its shared blocks, the levels of a track matrix, the shared
    /// blocks, and every competency of the version with the compositions of the blocks as the version counts them.
    /// </summary>
    private async Task<LoadedEntries> LoadAsync(MatrixVersion version, MatrixContent content, CancellationToken cancellationToken)
    {
        var blockIds = content.SharedBlocks.Select(block => block.BlockId).ToList();
        var blocks = blockIds.Count == 0
            ? new Dictionary<Guid, CompetencyBlock>()
            : await context.Set<CompetencyBlock>().AsNoTracking().Where(block => blockIds.Contains(block.Id)).ToDictionaryAsync(block => block.Id, cancellationToken);
        var set = content.CompetenciesWith(version.CompositionsOf(blocks.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Guid>)pair.Value.CompetencyIds)));
        var competencyIds = set.Select(item => item.CompetencyId).Distinct().ToList();
        List<Guid> scaleIds = [version.ScaleId, .. blocks.Values.Select(block => block.ScaleId)];
        var levelIds = version.Matrix.TrackId is null ? [] : content.Levels.Select(level => level.LevelId).ToList();
        var scales = await context.Set<CompetencyScale>().AsNoTracking().Where(scale => scaleIds.Contains(scale.Id)).ToDictionaryAsync(scale => scale.Id, cancellationToken);
        return new LoadedEntries(
            scales[version.ScaleId],
            scales,
            levelIds.Count == 0
                ? new Dictionary<Guid, CareerLevel>()
                : await context.Set<CareerLevel>().AsNoTracking().Where(level => levelIds.Contains(level.Id)).ToDictionaryAsync(level => level.Id, cancellationToken),
            await context.Set<CareerCompetency>().AsNoTracking().Where(competency => competencyIds.Contains(competency.Id)).ToDictionaryAsync(competency => competency.Id, cancellationToken),
            blocks,
            set);
    }

    /// <summary>
    /// What a version refers to, read once for its checks.
    /// </summary>
    /// <param name="Scale">The version's scale.</param>
    /// <param name="Scales">The version's scale and the scales of its shared blocks, by identifier.</param>
    /// <param name="Levels">The levels of a track matrix the content lists, by identifier.</param>
    /// <param name="Competencies">Every competency of the version, by identifier.</param>
    /// <param name="Blocks">The shared blocks the content includes, by identifier.</param>
    /// <param name="Set">Every competency of the version with its group, in content order, a competency in two groups twice.</param>
    private sealed record LoadedEntries(
        CompetencyScale Scale,
        Dictionary<Guid, CompetencyScale> Scales,
        Dictionary<Guid, CareerLevel> Levels,
        Dictionary<Guid, CareerCompetency> Competencies,
        Dictionary<Guid, CompetencyBlock> Blocks,
        List<MatrixCompetency> Set)
    {
        public bool Includes(Guid competencyId) => Set.Exists(item => item.CompetencyId == competencyId);

        /// <summary>
        /// The requirements that count: those of competencies of the version.
        /// </summary>
        public IEnumerable<MatrixRequirement> Counted(IEnumerable<MatrixRequirement> requirements) =>
            requirements.Where(requirement => Includes(requirement.CompetencyId));

        /// <summary>
        /// Whether the competency counts on the version's scale rather than on the scale of a shared block, by its first group.
        /// </summary>
        public bool IsOnVersionScale(Guid competencyId) => BlockOf(competencyId) is null;

        public bool HasScore(MatrixRequirement requirement) =>
            requirement.RequiredScore is not { } score || ScaleOf(requirement.CompetencyId).Points.Any(point => point.Score == score);

        public string ScaleWords(Guid competencyId) => BlockOf(competencyId) is { } block ? $"шкале блока «{block.Name}»" : "шкале версии";

        public string CompetencyName(Guid competencyId) => Competencies.GetValueOrDefault(competencyId)?.Name ?? competencyId.ToString();

        private CompetencyScale ScaleOf(Guid competencyId) => BlockOf(competencyId) is { } block ? Scales[block.ScaleId] : Scale;

        private CompetencyBlock? BlockOf(Guid competencyId) =>
            Set.Find(item => item.CompetencyId == competencyId) is { BlockId: { } blockId } ? Blocks.GetValueOrDefault(blockId) : null;
    }
}
