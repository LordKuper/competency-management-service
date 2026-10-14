using System.Text.Json;

namespace Competency.CareerFramework;

/// <summary>
/// The content of a matrix version as one document: its levels with their readiness thresholds, the named blocks of its own, the
/// competencies of those blocks and outside blocks with the block each belongs to, the shared blocks it includes by reference, the
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
    /// The competencies of the version: its own and those outside blocks, then the composition of each shared block under that block.
    /// A competency in two groups is listed twice, so completeness can tell.
    /// </summary>
    /// <param name="compositions">The composition of each shared block, as <see cref="MatrixVersion.CompositionsOf"/> chooses it.</param>
    /// <returns>The competencies with their groups.</returns>
    public List<MatrixCompetency> CompetenciesWith(IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> compositions) =>
    [
        .. Competencies,
        .. SharedBlocks.SelectMany(block => compositions.GetValueOrDefault(block.BlockId, [])
            .Select(competencyId => new MatrixCompetency(competencyId, block.BlockId))),
    ];

    /// <summary>
    /// The document with the compositions of its shared blocks written into its competencies, as two documents are compared.
    /// </summary>
    /// <param name="compositions">The composition of each shared block, as <see cref="MatrixVersion.CompositionsOf"/> chooses it.</param>
    /// <returns>The expanded document.</returns>
    public MatrixContent Expanded(IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> compositions) => this with { Competencies = CompetenciesWith(compositions) };

    /// <summary>
    /// Checks the document on its own: no missing lists or items, no repeated keys, items refer to levels and blocks of the document, and
    /// numbers, requirement types and texts within their limits. What the document refers to outside itself, the competencies of shared
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
            || BlockWeights.Any(item => item is null) || Requirements.Any(item => item is null) || SharedBlocks.Any(item => item is null))
        {
            return "Документ версии заполнен не полностью";
        }

        var isUnique = Levels.DistinctBy(item => item.LevelId).Count() == Levels.Count
            && Blocks.Select(item => item.Id).Concat(SharedBlocks.Select(item => item.BlockId)).Distinct().Count() == Blocks.Count + SharedBlocks.Count
            && Competencies.DistinctBy(item => item.CompetencyId).Count() == Competencies.Count
            && BlockWeights.DistinctBy(item => (item.LevelId, item.BlockId)).Count() == BlockWeights.Count
            && Requirements.DistinctBy(item => (item.LevelId, item.CompetencyId)).Count() == Requirements.Count;
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
            ? "Требование относится к уровню не из версии"
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

        if (Requirements.Any(item => !Enum.IsDefined(item.Type)))
        {
            return "Укажите допустимый тип требования";
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
    /// The document as stored: texts trimmed, empty texts as null; call after <see cref="Validate"/> succeeded.
    /// </summary>
    /// <returns>The stored document.</returns>
    public MatrixContent Stored() => this with
    {
        Blocks = [.. Blocks.Select(item => item with { Name = item.Name.Trim() })],
        Requirements = [.. Requirements.Select(item => item with
        {
            ExpectedBehavior = OptionalText(item.ExpectedBehavior),
            EvidenceExamples = OptionalText(item.EvidenceExamples),
        })],
    };

    private static bool IsPercent(decimal? value) => value is null or (>= 0 and <= MaxPercent);

    private static string? OptionalText(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
