using System.Text.Json;

namespace Competency.CareerFramework;

/// <summary>
/// The content of a matrix version as one document: its levels with their readiness thresholds, the named blocks of its own, the
/// competencies of those blocks and outside blocks with the block each belongs to and its type in the version, the shared blocks it includes by reference, the
/// coefficients of each level and group, and the requirements of each level and competency. A position matrix has one level, keyed by its
/// position. Stored whole, so a version, a variant and a published version each hold a copy, and two copies compare item by item.
/// </summary>
internal sealed record MatrixContent(
    IReadOnlyList<MatrixLevel> Levels,
    IReadOnlyList<MatrixBlock> Blocks,
    IReadOnlyList<MatrixCompetency> Competencies,
    IReadOnlyList<MatrixBlockWeight> BlockWeights,
    IReadOnlyList<MatrixRequirement> Requirements,
    IReadOnlyList<MatrixSharedBlock>? SharedBlocks = null)
{
    public const int TextMaxLength = 4000;
    public const decimal MaxPercent = 100;

    /// <summary>
    /// The refusal of a requirement of a level or competency the version does not have.
    /// </summary>
    public const string ForeignRequirementMessage = "Требование относится к уровню или компетенции не из версии";

    private const string Field = "content";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static MatrixContent Empty { get; } = new([], [], [], [], []);

    /// <summary>
    /// The shared blocks the version includes by reference; empty in a document written before shared blocks existed or sent without them.
    /// </summary>
    public IReadOnlyList<MatrixSharedBlock> SharedBlocks { get; init; } = SharedBlocks ?? [];

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
    /// block under that block. A competency in two groups is listed twice, so completeness can tell; one without a type set is a profile one.
    /// </summary>
    /// <param name="compositions">The composition of each shared block, as <see cref="MatrixVersion.CompositionsOf"/> chooses it.</param>
    /// <returns>The competencies with their groups and types.</returns>
    public List<MatrixCompetency> CompetenciesWith(IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> compositions) =>
    [
        .. Competencies.Select(item => item with { Type = item.Type ?? CompetencyType.Profile }),
        .. SharedBlocks.SelectMany(block => compositions.GetValueOrDefault(block.BlockId, [])
            .Select(competencyId => new MatrixCompetency(competencyId, block.BlockId, block.TypeOf(competencyId)))),
    ];

    /// <summary>
    /// The document with the type of every competency set explicitly, as it is stored: the type sent, else the type the previous
    /// document had for the competency, else profile. A shared block keeps types for the competencies of its current composition only.
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
        CompetencyType TypeOf(Guid competencyId, CompetencyType? sent) => sent ?? storedTypes.GetValueOrDefault(competencyId, CompetencyType.Profile);

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
    /// Checks the document on its own: no missing lists or items, no repeated keys, items refer to levels and blocks of the document, and
    /// numbers, competency types and texts within their limits. What the document refers to outside itself, the competencies of shared
    /// blocks that requirements may name among them, is checked against the catalogues.
    /// </summary>
    /// <returns>The problems found by field name; empty when the document is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var problem = ShapeProblem() ?? ReferenceProblem() ?? ValueProblem();
        return problem is null ? [] : new Dictionary<string, string[]> { [Field] = [problem] };
    }

    private string? ShapeProblem()
    {
        if (Levels is null || Blocks is null || Competencies is null || BlockWeights is null || Requirements is null
            || Levels.Any(item => item is null) || Blocks.Any(item => item is null) || Competencies.Any(item => item is null)
            || BlockWeights.Any(item => item is null) || Requirements.Any(item => item is null) || SharedBlocks.Any(item => item is null)
            || SharedBlocks.Any(item => item.CompetencyTypes.Any(type => type is null)))
        {
            return "Документ версии заполнен не полностью";
        }

        var isUnique = Levels.DistinctBy(item => item.LevelId).Count() == Levels.Count
            && Blocks.Select(item => item.Id).Concat(SharedBlocks.Select(item => item.BlockId)).Distinct().Count() == Blocks.Count + SharedBlocks.Count
            && Competencies.DistinctBy(item => item.CompetencyId).Count() == Competencies.Count
            && BlockWeights.DistinctBy(item => (item.LevelId, item.BlockId)).Count() == BlockWeights.Count
            && Requirements.DistinctBy(item => (item.LevelId, item.CompetencyId)).Count() == Requirements.Count
            && SharedBlocks.All(block => block.CompetencyTypes.DistinctBy(item => item.CompetencyId).Count() == block.CompetencyTypes.Count);
        return isUnique ? null : "Уровни, блоки, компетенции и требования не должны повторяться";
    }

    private string? ReferenceProblem()
    {
        var levelIds = Levels.Select(item => item.LevelId).ToHashSet();
        var blockIds = Blocks.Select(item => item.Id).ToHashSet();
        var groupIds = blockIds.Concat(SharedBlocks.Select(item => item.BlockId)).ToHashSet();
        if (Competencies.Any(item => item.BlockId is { } blockId && !blockIds.Contains(blockId)))
        {
            return "Компетенция относится к блоку не из версии";
        }

        if (BlockWeights.Any(item => !levelIds.Contains(item.LevelId) || (item.BlockId is { } blockId && !groupIds.Contains(blockId))))
        {
            return "Коэффициент относится к уровню или блоку не из версии";
        }

        return Requirements.Any(item => !levelIds.Contains(item.LevelId))
            ? ForeignRequirementMessage
            : null;
    }

    private string? ValueProblem()
    {
        if (Blocks.Any(item => string.IsNullOrWhiteSpace(item.Name)))
        {
            return "Укажите название блока";
        }

        if (Blocks.Any(item => item.Name.Trim().Length > CatalogEntry.NameMaxLength))
        {
            return $"Название блока — не длиннее {CatalogEntry.NameMaxLength} символов";
        }

        if (Levels.Any(item => !IsPercent(item.MinOverallPercent))
            || BlockWeights.Any(item => !IsPercent(item.Percent) || !IsPercent(item.MinPercent)))
        {
            return $"Коэффициенты и пороги — от 0 до {MaxPercent} %";
        }

        if (Competencies.Any(item => item.Type is { } type && !Enum.IsDefined(type))
            || SharedBlocks.Any(block => block.CompetencyTypes.Any(item => !Enum.IsDefined(item.Type))))
        {
            return "Укажите допустимый тип компетенции";
        }

        if (Requirements.Any(item => item.Weight < 0))
        {
            return "Вес не может быть отрицательным";
        }

        return Requirements.Any(item => item.ExpectedBehavior?.Trim().Length > TextMaxLength || item.EvidenceExamples?.Trim().Length > TextMaxLength)
            ? $"Описание и примеры — не длиннее {TextMaxLength} символов"
            : null;
    }

    /// <summary>
    /// The document as stored: texts trimmed, empty texts as null, and requirements without a type, which a requirement no longer has;
    /// call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <returns>The stored document.</returns>
    public MatrixContent Stored() => this with
    {
        Blocks = [.. Blocks.Select(item => item with { Name = item.Name.Trim() })],
        Requirements = [.. Requirements.Select(item => item with
        {
            ExpectedBehavior = OptionalText(item.ExpectedBehavior),
            EvidenceExamples = OptionalText(item.EvidenceExamples),
            Type = null,
        })],
    };

    private static bool IsPercent(decimal? value) => value is null or (>= 0 and <= MaxPercent);

    private static string? OptionalText(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
