using System.Text.Json;

namespace Kalibr.CareerFramework;

/// <summary>
/// The content of a matrix version as one document: its levels with their readiness thresholds, the competencies outside blocks with
/// their type in the version, the shared blocks it includes by reference, the coefficients of each level and group, the weights of
/// competencies in the groups that have their own, and the required score of each level and competency. The groups are the shared blocks and the competencies outside blocks; a version has no blocks of
/// its own. A position matrix has one level, keyed by its position. Stored whole, so a version, a variant and a published version each
/// hold a copy, and two copies compare item by item.
/// </summary>
internal sealed record MatrixContent(
    IReadOnlyList<MatrixLevel> Levels,
    IReadOnlyList<MatrixCompetency> Competencies,
    IReadOnlyList<MatrixBlockWeight> BlockWeights,
    IReadOnlyList<MatrixRequirement> Requirements,
    IReadOnlyList<MatrixSharedBlock>? SharedBlocks = null,
    IReadOnlyList<MatrixCompetencyWeight>? CompetencyWeights = null)
{
    public const decimal MaxPercent = 100;

    /// <summary>
    /// The refusal of a requirement of a level or competency the version does not have.
    /// </summary>
    public const string ForeignRequirementMessage = "Требование относится к уровню или компетенции не из версии";

    private const string Field = "content";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static MatrixContent Empty { get; } = new([], [], [], []);

    /// <summary>
    /// The shared blocks the version includes by reference; empty when sent without them.
    /// </summary>
    public IReadOnlyList<MatrixSharedBlock> SharedBlocks { get; init; } = SharedBlocks ?? [];

    /// <summary>
    /// The weights of competencies in the groups that have their own; empty when sent without them, which weighs every group equally.
    /// </summary>
    public IReadOnlyList<MatrixCompetencyWeight> CompetencyWeights { get; init; } = CompetencyWeights ?? [];

    public static MatrixContent FromJson(string json) => JsonSerializer.Deserialize<MatrixContent>(json, Json) ?? Empty;

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>
    /// Whether the other document holds the same items in the same order.
    /// </summary>
    /// <param name="other">The document to compare with.</param>
    /// <returns>True when the documents are equal.</returns>
    public bool SameAs(MatrixContent other) => ToJson() == other.ToJson();

    /// <summary>
    /// The competencies of the version with their types in it: its own and those outside blocks, then the composition of each shared
    /// block under that block. A competency in two groups is listed twice, so completeness can tell; one without a type set is an additional one.
    /// </summary>
    /// <param name="compositions">The composition of each shared block, as <see cref="MatrixVersion.CompositionsOf"/> chooses it.</param>
    /// <returns>The competencies with their groups and types.</returns>
    public List<MatrixCompetency> CompetenciesWith(IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> compositions) =>
    [
        .. Competencies.Select(item => item with { Type = item.Type ?? CompetencyType.Additional }),
        .. SharedBlocks.SelectMany(block => compositions.GetValueOrDefault(block.BlockId, [])
            .Select(competencyId => new MatrixCompetency(competencyId, block.BlockId, block.TypeOf(competencyId)))),
    ];

    /// <summary>
    /// The document with the type of every competency set explicitly, as it is stored: the type sent, else the type the previous
    /// document had for the competency, else additional. A shared block keeps types for the competencies of its current composition only.
    /// </summary>
    /// <param name="previous">The document as stored before.</param>
    /// <param name="compositions">The current composition of each shared block of this document.</param>
    /// <returns>The typed document.</returns>
    public MatrixContent WithTypes(MatrixContent previous, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> compositions)
    {
        var storedTypes = previous.Competencies
            .Where(item => item.Type is not null)
            .Select(item => (item.CompetencyId, Type: item.Type!.Value))
            .Concat(previous.SharedBlocks.SelectMany(block => block.CompetencyTypes.Select(item => (item.CompetencyId, item.Type))))
            .DistinctBy(item => item.CompetencyId)
            .ToDictionary(item => item.CompetencyId, item => item.Type);
        CompetencyType TypeOf(Guid competencyId, CompetencyType? sent) => sent ?? storedTypes.GetValueOrDefault(competencyId, CompetencyType.Additional);

        return this with
        {
            Competencies = [.. Competencies.Select(item => item with { Type = TypeOf(item.CompetencyId, item.Type) })],
            SharedBlocks = [.. SharedBlocks.Select(block => block with
            {
                CompetencyTypes = [.. compositions.GetValueOrDefault(block.BlockId, [])
                    .Select(competencyId => new MatrixCompetencyType(
                        competencyId,
                        TypeOf(competencyId, block.CompetencyTypes.FirstOrDefault(item => item.CompetencyId == competencyId)?.Type)))],
            })],
        };
    }

    /// <summary>
    /// The document with the compositions of its shared blocks written into its competencies, as two documents are compared.
    /// </summary>
    /// <param name="compositions">The composition of each shared block, as <see cref="MatrixVersion.CompositionsOf"/> chooses it.</param>
    /// <returns>The expanded document.</returns>
    public MatrixContent Expanded(IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> compositions) => this with { Competencies = CompetenciesWith(compositions) };

    /// <summary>
    /// Checks the document on its own: no missing lists or items, no repeated keys, no competency in a block of the version's own, items
    /// refer to levels and shared blocks of the document, and numbers, education levels and competency types within their limits. What the document refers
    /// to outside itself, the competencies of shared blocks that requirements may name among them, is checked against the catalogues.
    /// </summary>
    /// <returns>The problems found by field name; empty when the document is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var problem = ShapeProblem() ?? ReferenceProblem() ?? ValueProblem();
        return problem is null ? [] : new Dictionary<string, string[]> { [Field] = [problem] };
    }

    private string? ShapeProblem()
    {
        if (Levels is null || Competencies is null || BlockWeights is null || Requirements is null
            || Levels.Any(item => item is null) || Competencies.Any(item => item is null)
            || BlockWeights.Any(item => item is null) || Requirements.Any(item => item is null) || SharedBlocks.Any(item => item is null)
            || CompetencyWeights.Any(item => item is null)
            || SharedBlocks.Any(item => item.CompetencyTypes.Any(type => type is null)))
        {
            return "Документ версии заполнен не полностью";
        }

        var isUnique = Levels.DistinctBy(item => item.LevelId).Count() == Levels.Count
            && SharedBlocks.DistinctBy(item => item.BlockId).Count() == SharedBlocks.Count
            && Competencies.DistinctBy(item => item.CompetencyId).Count() == Competencies.Count
            && BlockWeights.DistinctBy(item => (item.LevelId, item.BlockId)).Count() == BlockWeights.Count
            && Requirements.DistinctBy(item => (item.LevelId, item.CompetencyId)).Count() == Requirements.Count
            && CompetencyWeights.DistinctBy(item => (item.LevelId, item.CompetencyId)).Count() == CompetencyWeights.Count
            && SharedBlocks.All(block => block.CompetencyTypes.DistinctBy(item => item.CompetencyId).Count() == block.CompetencyTypes.Count);
        return isUnique ? null : "Уровни, блоки, компетенции и требования не должны повторяться";
    }

    private string? ReferenceProblem()
    {
        var levelIds = Levels.Select(item => item.LevelId).ToHashSet();
        var groupIds = SharedBlocks.Select(item => item.BlockId).ToHashSet();
        if (Competencies.Any(item => item.BlockId is not null))
        {
            return "Собственных блоков у версии нет: компетенцию добавляют вне блоков или в составе общего блока";
        }

        if (BlockWeights.Any(item => !levelIds.Contains(item.LevelId) || (item.BlockId is { } blockId && !groupIds.Contains(blockId))))
        {
            return "Коэффициент относится к уровню или блоку не из версии";
        }

        if (CompetencyWeights.Any(item => !levelIds.Contains(item.LevelId)))
        {
            return "Вес относится к уровню не из версии";
        }

        return Requirements.Any(item => !levelIds.Contains(item.LevelId))
            ? ForeignRequirementMessage
            : null;
    }

    private string? ValueProblem()
    {
        if (Levels.Any(item => !IsPercent(item.MinOverallPercent)) || BlockWeights.Any(item => !IsPercent(item.Percent))
            || CompetencyWeights.Any(item => !IsPercent(item.Percent)))
        {
            return $"Коэффициенты, веса и пороги — от 0 до {MaxPercent} %";
        }

        if (Levels.Any(item => item.RequiredEducation is { } education && !Enum.IsDefined(education)))
        {
            return "Укажите допустимый уровень образования";
        }

        return Competencies.Any(item => item.Type is { } type && !Enum.IsDefined(type))
            || SharedBlocks.Any(block => block.CompetencyTypes.Any(item => !Enum.IsDefined(item.Type)))
            ? "Укажите допустимый тип компетенции"
            : null;
    }

    /// <summary>
    /// The document with a requirement for every level and competency of the version: each pair without one gets the lowest score of the
    /// competency's scale, after the requirements already there, by level in level order and within a level in competency order. The
    /// requirements already there stay as they are, so the version never has a level or competency without a required score.
    /// </summary>
    /// <param name="compositions">The composition of each shared block, as <see cref="MatrixVersion.CompositionsOf"/> chooses it.</param>
    /// <param name="lowestScores">The lowest score of each competency's scale, by competency; a competency missing here gets none.</param>
    /// <returns>The filled document.</returns>
    public MatrixContent Filled(IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> compositions, IReadOnlyDictionary<Guid, int> lowestScores)
    {
        var present = Requirements.Select(item => (item.LevelId, item.CompetencyId)).ToHashSet();
        var competencyIds = CompetenciesWith(compositions).Select(item => item.CompetencyId).Distinct().ToList();
        var added = Levels.SelectMany(level => competencyIds
            .Where(competencyId => !present.Contains((level.LevelId, competencyId)) && lowestScores.ContainsKey(competencyId))
            .Select(competencyId => new MatrixRequirement(level.LevelId, competencyId, lowestScores[competencyId])));
        return this with { Requirements = [.. Requirements, .. added] };
    }

    /// <summary>
    /// The document filled by the shared blocks, competencies and scales of the entries the version refers to.
    /// </summary>
    /// <param name="entries">The entries, frozen at publication or read live.</param>
    /// <returns>The filled document.</returns>
    public MatrixContent Filled(MatrixEntries entries) => Filled(entries.Compositions(), entries.LowestScores());

    /// <summary>
    /// The groups whose competency weights do not fit them on a level: some but not all of the group's competencies weighted, or weights
    /// not adding up to 100 %. A group with none on a level weighs its competencies equally and is not listed.
    /// </summary>
    /// <param name="competencies">The competencies of the version with their groups, as <see cref="CompetenciesWith"/> lists them.</param>
    /// <returns>The level and group, the group being null for the competencies outside blocks.</returns>
    public IEnumerable<(Guid LevelId, Guid? BlockId)> MismatchedWeights(IReadOnlyList<MatrixCompetency> competencies) =>
        from level in CompetencyWeights.GroupBy(weight => weight.LevelId)
        from members in competencies.GroupBy(item => item.BlockId)
        let ids = members.Select(item => item.CompetencyId).ToHashSet()
        let own = level.Where(weight => ids.Contains(weight.CompetencyId)).ToList()
        where own.Count > 0 && (own.Count != ids.Count || own.Sum(weight => weight.Percent) != MaxPercent)
        select (level.Key, members.Key);

    /// <summary>
    /// Whether the competencies are all critical, so that readiness is decided by them alone and no overall threshold applies.
    /// </summary>
    /// <param name="competencies">The competencies of the version with their groups and types.</param>
    /// <returns>True when there are competencies and every one is critical.</returns>
    public static bool AreAllCritical(IReadOnlyList<MatrixCompetency> competencies) =>
        competencies.Count > 0 && competencies.All(item => item.Type == CompetencyType.Critical);

    /// <summary>
    /// Whether a level has a threshold the previous document did not have or had at another value.
    /// </summary>
    /// <param name="previous">The document as stored before.</param>
    /// <returns>True when a threshold is set or changed.</returns>
    public bool SetsThreshold(MatrixContent previous) => Levels.Any(level => level.MinOverallPercent is not null
        && previous.Levels.FirstOrDefault(item => item.LevelId == level.LevelId)?.MinOverallPercent != level.MinOverallPercent);

    private static bool IsPercent(decimal? value) => value is null or (>= 0 and <= MaxPercent);
}
