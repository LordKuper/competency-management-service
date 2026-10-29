namespace Competency.CareerFramework;

/// <summary>
/// One item that differs between two documents of a version, named by the keys of its kind: a level, a shared
/// block, or a competency by its own identifier, a block coefficient by level and group, a competency weight or a requirement by level and competency.
/// </summary>
internal sealed record MatrixChange(MatrixChangeKind Kind, MatrixChangeType Type, Guid? LevelId, Guid? BlockId, Guid? CompetencyId)
{
    /// <summary>
    /// The items added, removed or changed from one document to the next. Documents are compared with the compositions of their shared
    /// blocks written into their competencies (<see cref="MatrixContent.Expanded"/>), so a change of a composition shows as competencies
    /// of the block added and removed; a competency listed twice counts by its first group.
    /// </summary>
    /// <param name="before">The earlier document.</param>
    /// <param name="after">The later document.</param>
    /// <returns>The changes, by kind.</returns>
    public static List<MatrixChange> Between(MatrixContent before, MatrixContent after) =>
    [
        .. Compare(before.Levels, after.Levels, item => item.LevelId, key => new(MatrixChangeKind.Level, default, key, null, null)),
        .. Compare(IdsOf(before.SharedBlocks), IdsOf(after.SharedBlocks), id => id, key => new(MatrixChangeKind.Block, default, null, key, null)),
        .. Compare(before.Competencies, after.Competencies, item => item.CompetencyId, key => new(MatrixChangeKind.Competency, default, null, null, key)),
        .. Compare(before.BlockWeights, after.BlockWeights, item => (item.LevelId, item.BlockId), key => new(MatrixChangeKind.BlockWeight, default, key.LevelId, key.BlockId, null)),
        .. Compare(before.CompetencyWeights, after.CompetencyWeights, item => (item.LevelId, item.CompetencyId), key => new(MatrixChangeKind.CompetencyWeight, default, key.LevelId, null, key.CompetencyId)),
        .. Compare(before.Requirements, after.Requirements, item => (item.LevelId, item.CompetencyId), key => new(MatrixChangeKind.Requirement, default, key.LevelId, null, key.CompetencyId)),
    ];

    /// <summary>
    /// The shared blocks a document includes, compared by reference alone: the types of their competencies show as changed competencies.
    /// </summary>
    private static List<Guid> IdsOf(IReadOnlyList<MatrixSharedBlock> blocks) => [.. blocks.Select(block => block.BlockId)];

    private static IEnumerable<MatrixChange> Compare<TItem, TKey>(
        IReadOnlyList<TItem> before,
        IReadOnlyList<TItem> after,
        Func<TItem, TKey> keyOf,
        Func<TKey, MatrixChange> changeOf)
        where TKey : notnull
    {
        var earlier = before.DistinctBy(keyOf).ToDictionary(keyOf);
        var later = after.DistinctBy(keyOf).ToDictionary(keyOf);
        foreach (var (key, item) in later)
        {
            if (!earlier.TryGetValue(key, out var previous))
            {
                yield return changeOf(key) with { Type = MatrixChangeType.Added };
            }
            else if (!Equals(previous, item))
            {
                yield return changeOf(key) with { Type = MatrixChangeType.Changed };
            }
        }

        foreach (var key in earlier.Keys.Where(key => !later.ContainsKey(key)))
        {
            yield return changeOf(key) with { Type = MatrixChangeType.Removed };
        }
    }
}
