using System.Globalization;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The catalogue side of matrix versions: which entries a version may refer to, whether a version is complete enough to be handed on,
/// and the entries it refers to as they stand. A track matrix version refers to the levels of its track; a position matrix version has
/// one column, its position. A version refers to shared blocks available to the matrix unit and to competencies of any scale, for a track
/// matrix of the matrix direction or general ones, and to entries that are active or drafts whose author heads the matrix unit. A draft
/// shared block is also usable when it applies to a unit the matrix unit's head heads, since that head sees it whoever wrote it. Draft
/// levels of the matrix track are usable whoever wrote them, because a matrix may be built on a draft track of its unit, which its HR edits
/// too. Neither a version nor a shared block has a scale: a required score is a point of the competency's own scale.
/// The competencies of a version include the compositions of its shared blocks; a requirement of a competency that has left its block
/// counts nowhere.
/// </summary>
internal sealed class MatrixCatalog(AppDbContext context, IEmployeeDirectory employees, ProcessRoles roles, CatalogStore store)
{
    private const string ContentField = "content";
    private const string ForeignLevelMessage = "Уровень не из трека матрицы";
    private const string PositionColumnMessage = "У матрицы должности один столбец требований — её должность";
    private const string ForeignCompetencyMessage = "Компетенция не из области матрицы";

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
        else if (competencies.Any(competency => competency is not null && !IsOfDirection(competency, matrix)))
        {
            problem = ForeignCompetencyMessage;
        }
        else if (referenced.Any(entry => entry is null || (!previousIds.Contains(entry.Id) && !IsUsable(entry, headId, entries))))
        {
            problem = CatalogEndpoints.UnavailableMessage;
        }
        else if (blocks.FirstOrDefault(block => !previousIds.Contains(block!.Id) && !entries.IsAvailable(block)) is { } unavailable)
        {
            problem = UnavailableBlockMessage(unavailable);
        }
        else if (NewDuplicate(content, previous, entries) is { } duplicate)
        {
            problem = $"Компетенция «{entries.CompetencyName(duplicate)}» уже входит в версию";
        }
        else if (content.Requirements.Any(requirement => !entries.Includes(requirement.CompetencyId)
                     && !previousRequirements.Contains((requirement.LevelId, requirement.CompetencyId))))
        {
            problem = MatrixContent.ForeignRequirementMessage;
        }
        else if (entries.Counted(content.Requirements).FirstOrDefault(requirement => !entries.HasScore(requirement)) is { } outOfScale)
        {
            problem = $"Требуемого уровня нет в {entries.ScaleWords(outOfScale.CompetencyId)}";
        }

        return problem is null ? null : new Dictionary<string, string[]> { [ContentField] = [problem] };
    }

    /// <summary>
    /// Says what keeps the version from being handed on. A track matrix without levels has that problem alone. For both kinds: no
    /// competencies, coefficients of a column that do not add up to 100 %, a competency in two groups, an entry that is no longer usable or
    /// no longer fits the version, a shared block no longer available to the matrix unit, or a required score the competency's scale no
    /// longer has. For a track matrix also a level of the matrix track without a linked active position; for a position matrix a position
    /// no longer available to the matrix unit. Every level and competency has a required score, so none is checked for; an inactive scale
    /// is no problem.
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
        var problems = new List<MatrixProblem>();
        if (entries.Set.Count == 0)
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.NoCompetencies,
                matrix.PositionId is null ? "В матрице нет компетенций" : "В матрице должности нет компетенций"));
        }

        problems.AddRange(matrix.PositionId is { } positionId
            ? await PositionProblemsAsync(matrix, positionId, content, cancellationToken)
            : await TrackProblemsAsync(matrix, content, entries, cancellationToken));

        foreach (var competencyId in entries.Set.GroupBy(item => item.CompetencyId).Where(group => group.Count() > 1).Select(group => group.Key))
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.DuplicateCompetency,
                $"Компетенция «{entries.CompetencyName(competencyId)}» входит в версию дважды",
                CompetencyId: competencyId));
        }

        problems.AddRange(EntryProblems(version, entries, await HeadOfAsync(matrix, cancellationToken)));
        foreach (var requirement in entries.Counted(content.Requirements).Where(requirement => !entries.HasScore(requirement)))
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

        return new MatrixEntries(
            direction is null ? null : new MatrixNamedEntry(direction.Id, direction.Name, direction.Status),
            track is null ? null : new MatrixNamedEntry(track.Id, track.Name, track.Status),
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
                    competency.DirectionId,
                    entries.TypeOf(competency.Id),
                    CompetencyVersion.NumberOf(competency),
                    competency.Status,
                    entries.ScaleOf(competency.Id)?.Id,
                    competency.EvidenceExamples))],
            position is null ? null : new MatrixNamedEntry(position.Id, position.Name, position.IsActive ? CatalogStatus.Active : CatalogStatus.Inactive),
            [.. version.Content.SharedBlocks
                .Select(item => entries.Blocks.GetValueOrDefault(item.BlockId))
                .OfType<CompetencyBlock>()
                .Select(block => new MatrixSharedBlockEntry(block.Id, block.Name, block.Status, block.CompetencyIds))],
            [.. entries.Set
                .Select(item => entries.ScaleOf(item.CompetencyId))
                .OfType<CompetencyScale>()
                .DistinctBy(scale => scale.Id)
                .Select(ScaleEntryOf)]);
    }

    /// <summary>
    /// A document of the version with every missing requirement filled in (<see cref="MatrixContent.Filled(IReadOnlyDictionary{Guid, IReadOnlyList{Guid}}, IReadOnlyDictionary{Guid, int})"/>)
    /// by the scales the version counts with: frozen into the snapshot of a published or archived version, and read live otherwise.
    /// </summary>
    /// <param name="version">The version the document belongs to: its content or one of its variants.</param>
    /// <param name="content">The document.</param>
    /// <param name="compositions">The composition of each shared block, as <see cref="MatrixVersion.CompositionsOf"/> chooses it.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The filled document.</returns>
    public async Task<MatrixContent> FilledAsync(
        MatrixVersion version,
        MatrixContent content,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> compositions,
        CancellationToken cancellationToken) =>
        content.Filled(compositions, version.Snapshot?.LowestScores() ?? await LowestScoresAsync(content.CompetenciesWith(compositions), cancellationToken));

    /// <summary>
    /// A competency the saved content holds twice where at least one of the two places is new: an own or outside competency not in the
    /// previous content, or a shared block it did not include. A duplicate already there stays a completeness problem.
    /// </summary>
    private static Guid? NewDuplicate(MatrixContent content, MatrixContent previous, LoadedEntries entries)
    {
        var sharedNow = content.SharedBlocks.Select(block => block.BlockId).ToHashSet();
        var sharedBefore = previous.SharedBlocks.Select(block => block.BlockId).ToHashSet();
        var ownBefore = previous.Competencies.Select(item => item.CompetencyId).ToHashSet();
        bool IsNew(MatrixCompetency item) => item.BlockId is { } blockId && sharedNow.Contains(blockId)
            ? !sharedBefore.Contains(blockId)
            : !ownBefore.Contains(item.CompetencyId);

        return entries.Set
            .GroupBy(item => item.CompetencyId)
            .Where(group => group.Count() > 1 && group.Any(IsNew))
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();
    }

    private static string UnavailableBlockMessage(CompetencyBlock block) => $"Общий блок «{block.Name}» недоступен подразделению матрицы";

    private static MatrixScaleEntry ScaleEntryOf(CompetencyScale scale) => new(scale.Id, scale.Name, scale.Points, scale.Status);

    private static string? LevelProblem(CompetencyMatrix matrix, MatrixContent content, LoadedEntries entries) => matrix.PositionId is { } positionId
        ? content.Levels is [{ } column] && column.LevelId == positionId ? null : PositionColumnMessage
        : content.Levels.Any(item => entries.Levels.GetValueOrDefault(item.LevelId)?.TrackId != matrix.TrackId) ? ForeignLevelMessage : null;

    private static IEnumerable<MatrixProblem> EntryProblems(MatrixVersion version, LoadedEntries entries, Guid? headId)
    {
        IEnumerable<CatalogEntry> all = [.. entries.Levels.Values, .. entries.Competencies.Values, .. entries.Blocks.Values];
        foreach (var entry in all.Where(entry => !IsUsable(entry, headId, entries)))
        {
            yield return new MatrixProblem(
                MatrixProblemCode.InactiveEntry,
                $"В версии есть неактивная запись «{entry.Name}»",
                entry is CareerLevel ? entry.Id : null,
                entry is CareerCompetency ? entry.Id : null);
        }

        foreach (var competency in entries.Competencies.Values.Where(competency => !IsOfDirection(competency, version.Matrix)))
        {
            yield return new MatrixProblem(MatrixProblemCode.ForeignCompetency, $"Компетенция «{competency.Name}» не из области матрицы", CompetencyId: competency.Id);
        }

        foreach (var block in entries.Blocks.Values.Where(block => !entries.IsAvailable(block)))
        {
            yield return new MatrixProblem(MatrixProblemCode.SharedBlockUnavailable, UnavailableBlockMessage(block));
        }
    }

    /// <summary>
    /// Whether the version may refer to the entry: an active one, a draft level, or a draft the matrix unit's head may see as its author
    /// or, for a shared block, as the head of the unit it applies to.
    /// </summary>
    private static bool IsUsable(CatalogEntry entry, Guid? headId, LoadedEntries entries) =>
        entry.Status == CatalogStatus.Active
        || (entry.Status == CatalogStatus.Draft
            && (entry is CareerLevel
                || (headId is not null
                    && (entry.AuthorId == headId || (entry is CompetencyBlock { ScopeUnitId: { } scopeUnitId } && entries.HeadedScopeIds.Contains(scopeUnitId))))));

    private static bool IsOfDirection(CareerCompetency competency, CompetencyMatrix matrix) =>
        matrix.DirectionId is null || competency.DirectionId is null || competency.DirectionId == matrix.DirectionId;

    /// <summary>
    /// What keeps a track matrix version from being handed on by its levels: coefficients of a level not adding up to 100 %, levels
    /// without an active position.
    /// </summary>
    private async Task<List<MatrixProblem>> TrackProblemsAsync(
        CompetencyMatrix matrix,
        MatrixContent content,
        LoadedEntries entries,
        CancellationToken cancellationToken)
    {
        var problems = new List<MatrixProblem>();
        foreach (var level in content.Levels)
        {
            var levelId = level.LevelId;
            var name = entries.Levels.GetValueOrDefault(levelId)?.Name ?? levelId.ToString();
            var sum = WeightSum(content, levelId);
            if (sum != MatrixContent.MaxPercent)
            {
                problems.Add(new MatrixProblem(
                    MatrixProblemCode.BlockWeights,
                    $"Коэффициенты блоков уровня «{name}» дают в сумме {sum.ToString("0.##", DecimalComma)} %, нужно 100 %",
                    levelId));
            }
        }

        var readiness = await store.ReadinessAsync(matrix.TrackId!.Value, cancellationToken);
        problems.AddRange(readiness.LevelsWithoutActivePosition.Select(level => new MatrixProblem(
            MatrixProblemCode.LevelWithoutActivePosition,
            $"У уровня «{level.Name}» трека «{level.Track.Name}» нет связанной действующей должности",
            level.Id)));
        return problems;
    }

    /// <summary>
    /// What keeps a position matrix version from being handed on by its one column and its position: coefficients not adding up to
    /// 100 %, and a position that is inactive or no longer on the lists of the matrix unit and the units above it.
    /// </summary>
    private async Task<List<MatrixProblem>> PositionProblemsAsync(
        CompetencyMatrix matrix,
        Guid positionId,
        MatrixContent content,
        CancellationToken cancellationToken)
    {
        var problems = new List<MatrixProblem>();
        var sum = WeightSum(content, positionId);
        if (sum != MatrixContent.MaxPercent)
        {
            problems.Add(new MatrixProblem(
                MatrixProblemCode.BlockWeights,
                $"Коэффициенты блоков дают в сумме {sum.ToString("0.##", DecimalComma)} %, нужно 100 %",
                positionId));
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
    /// The account of the head leading the matrix unit's matrices, whose drafts the matrix may use; null when no such head can act.
    /// </summary>
    private Task<Guid?> HeadOfAsync(CompetencyMatrix matrix, CancellationToken cancellationToken) =>
        roles.MatrixHeadAccountAsync(matrix.OrgUnitId, cancellationToken);

    /// <summary>
    /// The lowest score of the current scale of each competency, by competency; one on a scale without points has none.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, int>> LowestScoresAsync(IEnumerable<MatrixCompetency> competencies, CancellationToken cancellationToken)
    {
        var ids = competencies.Select(item => item.CompetencyId).Distinct().ToList();
        var scales = await context.Set<CareerCompetency>()
            .AsNoTracking()
            .Where(competency => ids.Contains(competency.Id))
            .Join(context.Set<CompetencyScale>(), competency => competency.ScaleId, scale => scale.Id, (competency, scale) => new { competency.Id, scale.Points })
            .ToListAsync(cancellationToken);
        return scales.Where(item => item.Points.Count > 0).ToDictionary(item => item.Id, item => item.Points.Min(point => point.Score));
    }

    private async Task<PositionStatus?> PositionAsync(Guid positionId, CancellationToken cancellationToken) =>
        (await employees.FindOrgSnapshotAsync(cancellationToken)).Positions.GetValueOrDefault(positionId);

    /// <summary>
    /// Reads what the content refers to: the levels of a track matrix, the shared blocks, every competency of the version with the
    /// compositions of the blocks as the version counts them, the scales of those competencies, and, when a block applies to a
    /// unit, the matrix unit with the units above it, which decide what blocks are available.
    /// </summary>
    private async Task<LoadedEntries> LoadAsync(MatrixVersion version, MatrixContent content, CancellationToken cancellationToken)
    {
        var blockIds = content.SharedBlocks.Select(block => block.BlockId).ToList();
        var blocks = blockIds.Count == 0
            ? new Dictionary<Guid, CompetencyBlock>()
            : await context.Set<CompetencyBlock>().AsNoTracking().Where(block => blockIds.Contains(block.Id)).ToDictionaryAsync(block => block.Id, cancellationToken);
        var set = content.CompetenciesWith(version.CompositionsOf(blocks.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Guid>)pair.Value.CompetencyIds)));
        var competencyIds = set.Select(item => item.CompetencyId).Distinct().ToList();
        var levelIds = version.Matrix.TrackId is null ? [] : content.Levels.Select(level => level.LevelId).ToList();
        var competencies = await context.Set<CareerCompetency>()
            .AsNoTracking()
            .Where(competency => competencyIds.Contains(competency.Id))
            .ToDictionaryAsync(competency => competency.Id, cancellationToken);
        var scaleIds = competencies.Values.Select(competency => competency.ScaleId).Distinct().ToList();
        var scales = await context.Set<CompetencyScale>().AsNoTracking().Where(scale => scaleIds.Contains(scale.Id)).ToDictionaryAsync(scale => scale.Id, cancellationToken);
        var ancestry = blocks.Values.Any(block => block.ScopeUnitId is not null)
            ? await employees.FindAncestryAsync(version.Matrix.OrgUnitId, cancellationToken)
            : [];
        var leadingHeadlessCount = ancestry.TakeWhile(unit => unit.HeadEmployeeId is null).Count();
        var headEmployeeId = ancestry.ElementAtOrDefault(leadingHeadlessCount)?.HeadEmployeeId;
        return new LoadedEntries(
            scales,
            levelIds.Count == 0
                ? new Dictionary<Guid, CareerLevel>()
                : await context.Set<CareerLevel>().AsNoTracking().Where(level => levelIds.Contains(level.Id)).ToDictionaryAsync(level => level.Id, cancellationToken),
            competencies,
            blocks,
            set,
            ancestry.Select(unit => unit.Id).ToHashSet(),
            ancestry
                .Select((unit, index) => (Unit: unit, IsHeadless: index < leadingHeadlessCount))
                .Where(pair => pair.Unit.IsActive && headEmployeeId is not null && (pair.IsHeadless || pair.Unit.HeadEmployeeId == headEmployeeId))
                .Select(pair => pair.Unit.Id)
                .ToHashSet());
    }

    /// <summary>
    /// What a version refers to, read once for its checks.
    /// </summary>
    /// <param name="Scales">The scales of the version's competencies, by identifier.</param>
    /// <param name="Levels">The levels of a track matrix the content lists, by identifier.</param>
    /// <param name="Competencies">Every competency of the version, by identifier.</param>
    /// <param name="Blocks">The shared blocks the content includes, by identifier.</param>
    /// <param name="Set">Every competency of the version with its group, in content order, a competency in two groups twice.</param>
    /// <param name="AvailableScopeIds">The matrix unit and the units above it, read when a shared block applies to a unit.</param>
    /// <param name="HeadedScopeIds">The units among them the head leading the matrix unit's matrices heads or leads, whose draft blocks the head sees.</param>
    private sealed record LoadedEntries(
        Dictionary<Guid, CompetencyScale> Scales,
        Dictionary<Guid, CareerLevel> Levels,
        Dictionary<Guid, CareerCompetency> Competencies,
        Dictionary<Guid, CompetencyBlock> Blocks,
        List<MatrixCompetency> Set,
        HashSet<Guid> AvailableScopeIds,
        HashSet<Guid> HeadedScopeIds)
    {
        /// <summary>
        /// Whether a matrix of the unit may include the block: one that applies to all, or to the matrix unit or a unit above it.
        /// </summary>
        public bool IsAvailable(CompetencyBlock block) => block.ScopeUnitId is not { } scopeUnitId || AvailableScopeIds.Contains(scopeUnitId);

        public bool Includes(Guid competencyId) => Set.Exists(item => item.CompetencyId == competencyId);

        /// <summary>
        /// The requirements that count: those of competencies of the version.
        /// </summary>
        public IEnumerable<MatrixRequirement> Counted(IEnumerable<MatrixRequirement> requirements) =>
            requirements.Where(requirement => Includes(requirement.CompetencyId));

        public bool HasScore(MatrixRequirement requirement) =>
            ScaleOf(requirement.CompetencyId)?.Points.Any(point => point.Score == requirement.RequiredScore) != false;

        public string ScaleWords(Guid competencyId) => $"шкале «{ScaleOf(competencyId)?.Name}»";

        public string CompetencyName(Guid competencyId) => Competencies.GetValueOrDefault(competencyId)?.Name ?? competencyId.ToString();

        /// <summary>
        /// The competency's type in the version, by its first group.
        /// </summary>
        public CompetencyType TypeOf(Guid competencyId) => Set.Find(item => item.CompetencyId == competencyId)?.Type ?? CompetencyType.Additional;

        /// <summary>
        /// The scale the competency's scores are points of, its own; null for a competency not read.
        /// </summary>
        public CompetencyScale? ScaleOf(Guid competencyId) =>
            Competencies.GetValueOrDefault(competencyId) is { } competency ? Scales.GetValueOrDefault(competency.ScaleId) : null;
    }
}
