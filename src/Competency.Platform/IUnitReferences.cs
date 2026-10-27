namespace Competency.Platform;

/// <summary>
/// What the modules that cannot be referenced by the unit module keep about a unit: it cannot be deleted while any of it refers to the unit,
/// and its responsible HR assignment goes with it.
/// Implementations are scoped to a request, because they share its database context.
/// </summary>
public interface IUnitReferences
{
    /// <summary>
    /// Describes what refers to the unit and keeps it from being deleted.
    /// </summary>
    /// <param name="unitId">The unit.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>One line per kind of referring record, addressed to the user; empty when nothing refers to the unit.</returns>
    Task<IReadOnlyList<string>> BlockersAsync(Guid unitId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the unit's responsible HR assignment. The removal is only tracked on the shared database context: the caller saves it in its own
    /// transaction, under the tree lock, and the removed assignment is journaled.
    /// </summary>
    /// <param name="unitId">The unit.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>A task that completes when the removal is tracked.</returns>
    Task ReleaseAsync(Guid unitId, CancellationToken cancellationToken);
}
